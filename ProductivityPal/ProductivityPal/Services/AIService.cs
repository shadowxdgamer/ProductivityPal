using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using ProductivityPal.Models;
using ProductivityPal.Extensions;

namespace ProductivityPal.Services
{
    public class AIService
    {
        private readonly HttpClient _client;
        private const string _apiKey = "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"; // TODO: Store your OpenRouter API key securely
        private const string _endpoint = "https://openrouter.ai/api/v1/chat/completions";
        private const string _siteUrl = "https://productivitypal.app"; // Replace with your actual site URL
        private const string _siteName = "ProductivityPal"; // Your app name
        
        public AIService()
        {
            _client = new HttpClient();
            
            // Add the required headers for OpenRouter
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                _client.DefaultRequestHeaders.Add("HTTP-Referer", _siteUrl);
                _client.DefaultRequestHeaders.Add("X-Title", _siteName);
            }
        }
        
        public async Task<(Priority priority, string group)> AnalyzeTask(string taskTitle, string taskDescription = "")
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                // Return default values if no API key is set
                return (DeterminePriorityLocally(taskTitle), "");
            }
            
            try
            {
                // Build the full task content to analyze
                string taskContent = taskTitle;
                if (!string.IsNullOrEmpty(taskDescription))
                {
                    taskContent += $"\nDescription: {taskDescription}";
                }
                
                // Request AI to analyze the task and return JSON with priority and group
                var request = new
                {
                    model = "google/gemini-2.5-pro-exp-03-25:free",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are a task analysis assistant. Analyze the task and respond with JSON in the format {\"priority\": \"Low|Medium|High\", \"group\": \"<1-2 word category>\"} where 'group' is a short 1-2 word category that best describes the task type/domain."
                        },
                        new
                        {
                            role = "user",
                            content = $"Analyze this task and determine its priority and group: '{taskContent}'"
                        }
                    },
                    response_format = new { type = "json_object" }
                };
                
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _client.PostAsync(_endpoint, content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                
                // Parse the response to get the priority and group
                using JsonDocument doc = JsonDocument.Parse(responseJson);
                
                var choicesElement = doc.RootElement.GetProperty("choices")[0];
                var messageElement = choicesElement.GetProperty("message");
                var contentElement = messageElement.GetProperty("content");
                
                // Parse the content as JSON to get the fields
                using JsonDocument contentDoc = JsonDocument.Parse(contentElement.GetString());
                string priorityStr = contentDoc.RootElement.GetProperty("priority").GetString();
                string group = contentDoc.RootElement.GetProperty("group").GetString();
                
                // Ensure group is not too long
                if (group.Split(' ').Length > 2)
                {
                    // Try to shorten the group to at most 2 words
                    var words = group.Split(' ');
                    group = string.Join(" ", words.Take(2));
                }
                
                // Parse priority
                Priority priority = priorityStr.ToLower() switch
                {
                    "high" => Priority.High,
                    "medium" => Priority.Medium,
                    "low" => Priority.Low,
                    _ => Priority.Medium
                };
                
                return (priority, group);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling AI API: {ex.Message}");
                return (DeterminePriorityLocally(taskTitle), "");
            }
        }
        
        public async Task<List<(string group, List<int> taskIds)>> SuggestGrouping(ObservableCollection<TaskCard> tasks)
        {
            if (string.IsNullOrEmpty(_apiKey) || tasks == null || tasks.Count < 2)
            {
                return new List<(string group, List<int> taskIds)>();
            }
            
            try
            {
                // Convert ObservableCollection to List for processing
                var tasksList = tasks.ToList();
                
                // Format tasks as a list for the AI
                var tasksFormatted = string.Join("\n", tasksList.Select(t => $"ID:{t.Id} | Title: {t.Title}" + 
                                                 (string.IsNullOrEmpty(t.Description) ? "" : $" | Description: {t.Description}")));
                
                var request = new
                {
                    model = "google/gemini-2.5-pro-exp-03-25:free",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are a task organization assistant. Analyze the list of tasks and suggest logical groupings. " +
                                     "Return your response as JSON in the format {\"groups\": [{\"name\": \"<1-2 word group name>\", \"taskIds\": [id1, id2, ...]}]}. " +
                                     "Each group name should be 1-2 words maximum. Only group tasks that are clearly related."
                        },
                        new
                        {
                            role = "user",
                            content = $"Analyze these tasks and suggest logical groupings:\n{tasksFormatted}"
                        }
                    },
                    response_format = new { type = "json_object" }
                };
                
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _client.PostAsync(_endpoint, content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                
                using JsonDocument doc = JsonDocument.Parse(responseJson);
                var choicesElement = doc.RootElement.GetProperty("choices")[0];
                var messageElement = choicesElement.GetProperty("message");
                var contentElement = messageElement.GetProperty("content");
                
                // Parse the returned groups JSON
                using JsonDocument groupsDoc = JsonDocument.Parse(contentElement.GetString());
                var groupsArray = groupsDoc.RootElement.GetProperty("groups");
                
                var result = new List<(string group, List<int> taskIds)>();
                
                foreach (var groupElement in groupsArray.EnumerateArray())
                {
                    string name = groupElement.GetProperty("name").GetString();
                    var taskIds = new List<int>();
                    
                    var taskIdsArray = groupElement.GetProperty("taskIds");
                    foreach (var taskId in taskIdsArray.EnumerateArray())
                    {
                        taskIds.Add(taskId.GetInt32());
                    }
                    
                    if (taskIds.Count > 0)
                    {
                        result.Add((name, taskIds));
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error suggesting groupings: {ex.Message}");
                return new List<(string group, List<int> taskIds)>();
            }
        }
        
        private Priority DeterminePriorityLocally(string taskTitle)
        {
            // Simple logic to determine priority without API
            var title = taskTitle.ToLower();
            
            if (title.Contains("urgent") || title.Contains("critical") || title.Contains("asap"))
            {
                return Priority.High;
            }
            else if (title.Contains("important") || title.Contains("soon"))
            {
                return Priority.Medium;
            }
            else
            {
                return Priority.Low;
            }
        }
    }
}