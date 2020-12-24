using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace WireGuardServerForWindows.Converters
{
    /// <summary>
    /// Allows hiding the last item in a list. The ItemsSource must be an <see cref="IList"/>.
    /// Since the converter needs both the whole list and the current item (to compare the indexes),
    /// and since a ConverterParameter can't be bound, use a multi-binding to send both pieces of relevant information.
    /// The item of interest must be first, and the list must be second.
    /// Optionally set the <see cref="InvisibleMode"/> parameter to determine the visibility of the hidden item. This can be set in resources,
    /// or, since this converter supports MarkupExtension, it can be set inline.
    /// For example:
    /// <code>
    /// <Canvas.Visibility>
    ///     <MultiBinding Converter="{converters:LastIndexVisibilityConverter InvisibleMode=Collapsed}">
    ///         <Binding/>
    ///         <Binding Path="DataContext" RelativeSource="{RelativeSource AncestorType=ItemsControl}"/>
    ///     </MultiBinding>
    /// </Canvas.Visibility>
    /// </code>
    /// </summary>
    public class LastIndexVisibilityConverter : MarkupExtension, IMultiValueConverter
    {
        #region IMultiValueConverter members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility result = Visibility.Visible;

            if (values?.Length >= 2)
            {
                if ((values[1] as IList)?[^1] == values[0])
                {
                    result = InvisibleMode;
                }
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region MarkupExtension members

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        #endregion

        #region Public properties

        public Visibility InvisibleMode { get; set; } = Visibility.Collapsed;

        #endregion
    }
}
