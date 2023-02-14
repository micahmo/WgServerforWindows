using System;
using System.Windows;
using WgServerforWindows.Models;

namespace WgServerforWindows.Controls
{
    /// <summary>
    /// Interaction logic for ServerConfigurationEditor.xaml
    /// </summary>
    public partial class ServerConfigurationEditorWindow : Window
    {
        public ServerConfigurationEditorWindow()
        {
            InitializeComponent();
        }

        protected override void OnActivated(EventArgs e)
        {
            WaitCursor.SetOverrideCursor(null);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            AppSettings.Instance.Tracker.Track(this);
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
