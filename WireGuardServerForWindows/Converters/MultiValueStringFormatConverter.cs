using System;
using System.Globalization;
using System.Windows.Data;

namespace WireGuardServerForWindows.Converters
{
    public class MultiValueStringFormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string result = default;

            if (parameter is string format)
            {
                result = string.Format(format, values);
            }

            return result?.Trim();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
