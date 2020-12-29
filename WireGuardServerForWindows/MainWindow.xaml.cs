using System.Windows;
using SharpConfig;
using WireGuardServerForWindows.Models;

namespace WireGuardServerForWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Never put quotes around config file values
            Configuration.OutputRawStringValues = true;

            MainWindowModel mainWindowModel = new MainWindowModel();
            mainWindowModel.PrerequisiteItems.Add(new WireGuardExePrerequisite());
            mainWindowModel.PrerequisiteItems.Add(new ServerConfigurationPrerequisite());
            mainWindowModel.PrerequisiteItems.Add(new ClientConfigurationsPrerequisite());
            mainWindowModel.PrerequisiteItems.Add(new TunnelServicePrerequisite());
            mainWindowModel.PrerequisiteItems.Add(new ServerStatusPrerequisite());

            DataContext = mainWindowModel;
        }
    }
}
