using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using ProductivityPal.Models;
using ProductivityPal.Services;
using ProductivityPal.Extensions;

namespace ProductivityPal.ViewModels
{
    // Parameter classes for commands that need multiple parameters
    public class CardPriorityParameter
    {
        public TaskCard Card { get; set; }
        public Priority Priority { get; set; }
    }

    public class CardGroupParameter
    {
        public TaskCard Card { get; set; }
        public string Group { get; set; }
    }

    public partial class MainViewModel : ObservableObject, IDropTarget
    {
        private readonly DatabaseService _databaseService;
        private readonly AIService _aiService;
        
        [ObservableProperty]
        private ObservableCollection<TaskList> _taskLists = new ObservableCollection<TaskList>();

        [ObservableProperty]
        private bool _isLoading;
        
        // Standard ICommand properties for methods that need multiple parameters
        public IRelayCommand<CardPriorityParameter> UpdateCardPriorityCommand { get; }
        public IRelayCommand<CardGroupParameter> UpdateCardGroupCommand { get; }
        
        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _aiService = new AIService();
            
            // Initialize commands that need multiple parameters
            UpdateCardPriorityCommand = new RelayCommand<CardPriorityParameter>(UpdateCardPriority);
            UpdateCardGroupCommand = new RelayCommand<CardGroupParameter>(UpdateCardGroup);
            
            // Load data
            LoadDataAsync();
        }
        
        private async Task LoadDataAsync()
        {
            IsLoading = true;
            
            try
            {
                // Create initial data if needed
                await _databaseService.CreateInitialDataIfNeededAsync();
                
                // Load all data
                var lists = await _databaseService.GetTaskListsAsync();
                
                // Update the observable collection
                TaskLists.Clear();
                foreach (var list in lists.OrderBy(l => l.Id))
                {
                    // Make sure Cards is ObservableCollection
                    if (list.Cards == null)
                    {
                        list.Cards = new ObservableCollection<TaskCard>();
                    }
                    TaskLists.Add(list);
                }
            }
            catch (Exception ex)
            {
                // Handle any errors
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private async Task AddCard(TaskList list)
        {
            if (list == null) return;
            
            // Get position for new card (at the end of the list)
            int position = list.Cards.Count > 0 ? list.Cards.Max(c => c.Position) + 1 : 0;
            
            var newCard = new TaskCard
            {
                Title = "New Task",
                Priority = Priority.Medium,
                ListId = list.Id,
                Position = position
            };
            
            // Add to the list and save to database first
            list.Cards.Add(newCard);
            await _databaseService.SaveTaskCardAsync(newCard);
            
            // Analyze in background
            _ = Task.Run(async () => 
            {
                try
                {
                    var analysis = await _aiService.AnalyzeTask(newCard.Title, newCard.Description);
                    
                    // Update the card with AI suggestions
                    newCard.Priority = analysis.priority;
                    newCard.Group = analysis.group;
                    
                    // Save updated card
                    await _databaseService.SaveTaskCardAsync(newCard);
                    
                    // If we have enough cards, try to find logical groupings
                    if (list.Cards.Count > 3)
                    {
                        await SuggestGroupingsForList(list);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in background analysis: {ex.Message}");
                }
            });
        }
        
        private async Task SuggestGroupingsForList(TaskList list)
        {
            try
            {
                // Pass the ObservableCollection directly
                var suggestions = await _aiService.SuggestGrouping(list.Cards);
                
                // Apply suggested groupings with explicit deconstruction types
                foreach ((string group, List<int> taskIds) in suggestions)
                {
                    foreach (var id in taskIds)
                    {
                        var card = list.Cards.FirstOrDefault(c => c.Id == id);
                        if (card != null && string.IsNullOrEmpty(card.Group))
                        {
                            card.Group = group;
                            await _databaseService.SaveTaskCardAsync(card);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error suggesting groupings: {ex.Message}");
            }
        }
        
        [RelayCommand]
        private async Task AddList()
        {
            var newList = new TaskList
            {
                Title = "New List"
            };
            
            // Make sure Cards is initialized as ObservableCollection
            newList.Cards = new ObservableCollection<TaskCard>();
            
            // Save to database first to get the ID
            await _databaseService.SaveTaskListAsync(newList);
            
            // Add to observable collection
            TaskLists.Add(newList);
        }
        
        [RelayCommand]
        private async Task EditCard(TaskCard card)
        {
            if (card == null) return;
            
            // Show dialog to edit card properties
            // This would be implemented with a dialog service
            
            // Save changes to database
            await _databaseService.SaveTaskCardAsync(card);
            
            // Re-analyze in background if title or description changed
            _ = Task.Run(async () =>
            {
                try
                {
                    var analysis = await _aiService.AnalyzeTask(card.Title, card.Description);
                    
                    // Only update if priority or group has changed
                    if (analysis.priority != card.Priority || analysis.group != card.Group)
                    {
                        card.Priority = analysis.priority;
                        card.Group = analysis.group;
                        await _databaseService.SaveTaskCardAsync(card);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in background analysis: {ex.Message}");
                }
            });
        }
        
        private async void UpdateCardPriority(CardPriorityParameter param)
        {
            if (param?.Card == null) return;
            
            param.Card.Priority = param.Priority;
            await _databaseService.SaveTaskCardAsync(param.Card);
        }
        
        private async void UpdateCardGroup(CardGroupParameter param)
        {
            if (param?.Card == null) return;
            
            param.Card.Group = param.Group;
            await _databaseService.SaveTaskCardAsync(param.Card);
        }
        
        [RelayCommand]
        private async Task DeleteCard(TaskCard card)
        {
            if (card == null) return;
            
            // Find which list contains this card
            foreach (var list in TaskLists)
            {
                if (list.Cards.Contains(card))
                {
                    list.Cards.Remove(card);
                    
                    // Update positions of remaining cards
                    for (int i = 0; i < list.Cards.Count; i++)
                    {
                        list.Cards[i].Position = i;
                        await _databaseService.SaveTaskCardAsync(list.Cards[i]);
                    }
                    
                    // Delete the card from database
                    await _databaseService.DeleteTaskCardAsync(card);
                    break;
                }
            }
        }
        
        [RelayCommand]
        private async Task SetPriorityLow(TaskCard card)
        {
            if (card == null) return;
            card.Priority = Priority.Low;
            await _databaseService.SaveTaskCardAsync(card);
        }

        [RelayCommand]
        private async Task SetPriorityMedium(TaskCard card)
        {
            if (card == null) return;
            card.Priority = Priority.Medium;
            await _databaseService.SaveTaskCardAsync(card);
        }

        [RelayCommand]
        private async Task SetPriorityHigh(TaskCard card)
        {
            if (card == null) return;
            card.Priority = Priority.High;
            await _databaseService.SaveTaskCardAsync(card);
        }
        
        #region IDropTarget Implementation
        
        public void DragOver(IDropInfo dropInfo)
        {
            // Allow dropping TaskCards
            if (dropInfo.Data is TaskCard)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = System.Windows.DragDropEffects.Move;
            }
        }

        public async void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is TaskCard sourceCard)
            {
                // Handle dropping a card
                var targetList = GetTaskListFromDropTarget(dropInfo);
                var sourceList = FindSourceList(sourceCard);
                
                if (targetList != null && sourceList != null)
                {
                    // Remove from source
                    sourceList.Cards.Remove(sourceCard);
                    
                    // Add to target
                    int targetIndex = dropInfo.InsertIndex;
                    
                    if (targetIndex >= 0 && targetIndex <= targetList.Cards.Count)
                    {
                        targetList.Cards.Insert(targetIndex, sourceCard);
                    }
                    else
                    {
                        targetList.Cards.Add(sourceCard);
                        targetIndex = targetList.Cards.Count - 1;
                    }
                    
                    // Update the card's list ID
                    sourceCard.ListId = targetList.Id;
                    
                    // Update positions of all cards in the target list
                    for (int i = 0; i < targetList.Cards.Count; i++)
                    {
                        targetList.Cards[i].Position = i;
                        await _databaseService.SaveTaskCardAsync(targetList.Cards[i]);
                    }
                    
                    // Update positions in source list if it's different
                    if (sourceList != targetList)
                    {
                        for (int i = 0; i < sourceList.Cards.Count; i++)
                        {
                            sourceList.Cards[i].Position = i;
                            await _databaseService.SaveTaskCardAsync(sourceList.Cards[i]);
                        }
                    }
                }
            }
        }
        
        private TaskList GetTaskListFromDropTarget(IDropInfo dropInfo)
        {
            if (dropInfo.TargetCollection is System.Collections.IList cards &&
                dropInfo.VisualTarget is System.Windows.Controls.ListBox listBox)
            {
                // Find the task list that owns this collection
                return TaskLists.FirstOrDefault(list => list.Cards == cards || list.Cards == listBox.ItemsSource);
            }
            return null;
        }
        
        private TaskList FindSourceList(TaskCard card)
        {
            return TaskLists.FirstOrDefault(list => list.Cards.Contains(card));
        }
        
        #endregion
    }
}