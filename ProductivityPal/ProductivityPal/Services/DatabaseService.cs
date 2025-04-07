using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProductivityPal.Extensions;
using ProductivityPal.Models;
using SQLite;

namespace ProductivityPal.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private readonly string _databasePath;

        public DatabaseService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolderPath = Path.Combine(appDataPath, "ProductivityPal");
            
            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }
            
            _databasePath = Path.Combine(appFolderPath, "tasks.db");
        }

        private async Task InitializeAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_databasePath);
            
            // Create tables if they don't exist
            await _database.CreateTableAsync<TaskList>();
            await _database.CreateTableAsync<TaskCard>();
        }

        public async Task<List<TaskList>> GetTaskListsAsync()
        {
            await InitializeAsync();
            
            // Get all lists
            var lists = await _database.Table<TaskList>().ToListAsync();
            
            // Get all cards and assign them to their respective lists
            var cards = await _database.Table<TaskCard>()
                .OrderBy(c => c.Position)
                .ToListAsync();
            
            foreach (var list in lists)
            {
                // Make sure to use the extension method to convert List to ObservableCollection
                var listCards = cards.Where(c => c.ListId == list.Id).ToList();
                list.Cards = listCards.ToObservableCollection();
            }
            
            return lists;
        }

        public async Task SaveTaskListAsync(TaskList list)
        {
            await InitializeAsync();
            
            if (list.Id == 0)
            {
                // Insert new list
                await _database.InsertAsync(list);
            }
            else
            {
                // Update existing list
                await _database.UpdateAsync(list);
            }

            // Make sure all cards have the correct ListId and are saved
            if (list.Cards != null)
            {
                foreach (var card in list.Cards)
                {
                    card.ListId = list.Id;
                    await SaveTaskCardAsync(card);
                }
            }
        }

        public async Task SaveTaskCardAsync(TaskCard card)
        {
            await InitializeAsync();
            
            if (card.Id == 0)
            {
                // Insert new card
                await _database.InsertAsync(card);
            }
            else
            {
                // Update existing card
                await _database.UpdateAsync(card);
            }
        }

        public async Task DeleteTaskCardAsync(TaskCard card)
        {
            await InitializeAsync();
            await _database.DeleteAsync(card);
        }

        public async Task DeleteTaskListAsync(TaskList list)
        {
            await InitializeAsync();
            
            // Delete all cards in this list first
            await _database.ExecuteAsync($"DELETE FROM TaskCard WHERE ListId = ?", list.Id);
            
            // Delete the list
            await _database.DeleteAsync(list);
        }

        public async Task<bool> CreateInitialDataIfNeededAsync()
        {
            await InitializeAsync();
            
            // Check if any data exists
            var count = await _database.Table<TaskList>().CountAsync();
            if (count > 0)
                return false; // Data already exists
                
            // Create default lists
            var todoList = new TaskList { Title = "TODO" };
            var doingList = new TaskList { Title = "DOING" };
            var doneList = new TaskList { Title = "DONE" };
            
            await _database.InsertAsync(todoList);
            await _database.InsertAsync(doingList);
            await _database.InsertAsync(doneList);
            
            // Add a sample card
            var sampleCard = new TaskCard
            {
                Title = "UI improvements",
                Description = "Improve the user interface design",
                Priority = Priority.High,
                ListId = todoList.Id,
                Position = 0
            };
            
            await _database.InsertAsync(sampleCard);
            
            // Initialize the Cards collection explicitly
            todoList.Cards = new ObservableCollection<TaskCard> { sampleCard };
            
            return true; // Initial data created
        }
    }
}