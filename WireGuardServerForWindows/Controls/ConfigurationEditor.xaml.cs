using System.Windows;

namespace WireGuardServerForWindows.Controls
{
    /// <summary>
    /// Interaction logic for ConfigurationEditor.xaml
    /// </summary>
    public partial class ConfigurationEditor : Window
    {
        public ConfigurationEditor()
        {
            InitializeComponent();
        }

        #region Event handlers

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion
    }
}
