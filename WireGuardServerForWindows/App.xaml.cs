using System.Windows;
using System.Windows.Threading;
using WireGuardServerForWindows.Controls;
using WireGuardServerForWindows.Models;

namespace WireGuardServerForWindows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += Application_DispatcherUnhandledException;
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            new UnhandledErrorWindow {DataContext = new UnhandledErrorWindowModel
            {
                Title = WireGuardServerForWindows.Properties.Resources.Error,
                Text = string.Format(WireGuardServerForWindows.Properties.Resources.UnexpectedErrorMessage, e.Exception.Message),
                Exception = e.Exception
            }}.ShowDialog();


            // Don't kill the app
            e.Handled = true;
        }
    }
}
