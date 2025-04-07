using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace ProductivityPal.Models
{
    public class TaskCard : INotifyPropertyChanged
    {
        private int _id;
        private string _title;
        private string _description;
        private Priority _priority;
        private string _group;
        private int _listId;
        private int _position;

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

        public string Description 
        { 
            get => _description; 
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public Priority Priority 
        { 
            get => _priority; 
            set
            {
                if (_priority != value)
                {
                    _priority = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Group 
        { 
            get => _group; 
            set
            {
                if (_group != value)
                {
                    _group = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ListId 
        { 
            get => _listId; 
            set
            {
                if (_listId != value)
                {
                    _listId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Position 
        { 
            get => _position; 
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum Priority { Low, Medium, High }
}
