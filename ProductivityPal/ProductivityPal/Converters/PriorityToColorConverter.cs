using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ProductivityPal.Models;

namespace ProductivityPal.Converters
{
    public class PriorityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Priority priority)
            {
                return priority switch
                {
                    Priority.Low => new SolidColorBrush(Colors.Green),
                    Priority.Medium => new SolidColorBrush(Colors.Yellow),
                    Priority.High => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}