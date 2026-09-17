using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Domain.Enum;

namespace Presentation_WPF.Converter
{
    public class TaskGroupStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not TaskGroupState state) return Brushes.Gray;
            return state switch
            {
                TaskGroupState.Waiting => new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)),
                TaskGroupState.InProgress => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                TaskGroupState.Completed => new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)),
                TaskGroupState.Canceled => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                _ => Brushes.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}