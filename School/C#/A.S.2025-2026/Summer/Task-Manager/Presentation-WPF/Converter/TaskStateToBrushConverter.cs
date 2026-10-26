using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Domain.Enum;

namespace Presentation_WPF.Converter
{
    public class TaskStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not TaskState state) return Brushes.Gray;
            return state switch
            {
                TaskState.Waiting => new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)),
                TaskState.Running => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                TaskState.Completed => new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)),
                TaskState.Canceled => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                _ => Brushes.Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}