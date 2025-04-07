using System;
using System.Globalization;
using System.Windows.Data;
using ProductivityPal.Models;
using ProductivityPal.ViewModels;

namespace ProductivityPal.Converters
{
    public class CardPriorityParameterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is TaskCard card && values[1] is Priority priority)
            {
                return new CardPriorityParameter
                {
                    Card = card,
                    Priority = priority
                };
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}