using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ProductivityPal.Extensions;
using SQLite;

namespace ProductivityPal.Models
{
    public class TaskList : INotifyPropertyChanged
    {
        private int _id;
        private string _title;
        private ObservableCollection<TaskCard> _cards;

        public TaskList()
        {
            _cards = new ObservableCollection<TaskCard>();
        }

        [PrimaryKey, AutoIncrement]
        public int Id 
        { 
            get => _id; 
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string Title 
        { 
            get => _title; 
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        [Ignore] 
        public ObservableCollection<TaskCard> Cards
        { 
            get => _cards ?? (_cards = new ObservableCollection<TaskCard>()); 
            set
            {
                if (value == null)
                {
                    _cards = new ObservableCollection<TaskCard>();
                    OnPropertyChanged();
                    return;
                }
                
                if (_cards != value)
                {
                    if (value is ObservableCollection<TaskCard> observableCards)
                    {
                        _cards = observableCards;
                    }
                    //else if (value is List<TaskCard> listCards)
                    //{
                    //    _cards = new ObservableCollection<TaskCard>(listCards);
                    //}
                    else if (value is IEnumerable<TaskCard> cards)
                    {
                        _cards = new ObservableCollection<TaskCard>(cards);
                    }
                    OnPropertyChanged();
                }
            }
        }

        // Helper method for explicitly adding cards
        public void AddCard(TaskCard card)
        {
            if (_cards == null)
            {
                _cards = new ObservableCollection<TaskCard>();
            }
            _cards.Add(card);
            OnPropertyChanged(nameof(Cards));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
