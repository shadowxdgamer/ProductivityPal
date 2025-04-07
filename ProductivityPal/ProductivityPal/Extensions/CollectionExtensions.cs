using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProductivityPal.Extensions
{
    public static class CollectionExtensions
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> items)
        {
            return new ObservableCollection<T>(items);
        }
    }
}