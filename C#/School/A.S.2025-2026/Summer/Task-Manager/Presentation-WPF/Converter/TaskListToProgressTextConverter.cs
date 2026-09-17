using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Domain.Enum;

namespace Presentation_WPF.Converter
{
    public class TaskListToProgressTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IEnumerable tasks) return string.Empty;
            var list = tasks.Cast<object>().ToList();
            int total = list.Count;
            int completed = 0;
            foreach (var item in list)
            {
                var prop = item.GetType().GetProperty("State");
                if (prop != null)
                {
                    var state = prop.GetValue(item);
                    if (state is TaskState ts && ts == TaskState.Completed)
                        completed++;
                }
            }
            return $"{completed} / {total} completati";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}