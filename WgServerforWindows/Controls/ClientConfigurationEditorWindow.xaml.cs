using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WgServerforWindows.Models;

namespace WgServerforWindows.Controls
{
    /// <summary>
    /// Interaction logic for ServerConfigurationEditor.xaml
    /// </summary>
    public partial class ClientConfigurationEditorWindow : Window
    {
        public ClientConfigurationEditorWindow()
        {
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            WaitCursor.SetOverrideCursor(null);
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

        private void ExplorerSearchBox_SearchRequested(object sender, string e)
        {
            if (DataContext is ClientConfigurationList clientConfigurationList)
            {
                clientConfigurationList.List.ToList().ForEach(c => c.IsVisible = true);

                if (!string.IsNullOrWhiteSpace(e))
                {
                    clientConfigurationList.List.Where(c => !c.Name.Contains(e, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(c => c.IsVisible = false);
                }

                clientConfigurationList.RaisePropertyChanged(nameof(clientConfigurationList.CountString));
            }
        }
    }
}
