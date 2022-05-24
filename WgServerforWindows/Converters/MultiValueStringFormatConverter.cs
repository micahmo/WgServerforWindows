using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace WgServerforWindows.Converters
{
    public class MultiValueStringFormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string result = default;

            if (parameter is string format)
            {
                result = string.Format(format, values.Select(v => v == DependencyProperty.UnsetValue ? string.Empty : v).ToArray());
            }

            return result?.Trim();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
