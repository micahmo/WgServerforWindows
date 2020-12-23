using System;
using System.Windows;
using System.Windows.Input;

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

        protected override void OnActivated(EventArgs e)
        {
            Mouse.OverrideCursor = null;
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
