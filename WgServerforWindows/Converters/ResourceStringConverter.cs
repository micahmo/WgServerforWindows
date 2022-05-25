using System;
using System.Globalization;
using System.Windows.Data;

namespace WgServerforWindows.Converters
{
    public class ResourceStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = default;

            try
            {
                result = Properties.Resources.ResourceManager.GetString(value?.ToString());
            }
            catch
            {
                // Ignore resource exceptions
            }
            finally
            {
                if (string.IsNullOrEmpty(result))
                {
                    result = $"***{value}***";
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Should never try to convert back.
            throw new NotImplementedException();
        }
    }
}
