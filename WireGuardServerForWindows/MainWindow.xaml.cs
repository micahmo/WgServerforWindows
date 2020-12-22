using System.Windows;
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

            MainWindowModel mainWindowModel = new MainWindowModel();
            mainWindowModel.PrerequisiteItems.Add(new WireGuardExePrerequisite());
            
            ServerConfigurationPrerequisite serverConfigurationPrerequisite = new ServerConfigurationPrerequisite();
            mainWindowModel.PrerequisiteItems.Add(serverConfigurationPrerequisite);
            mainWindowModel.PrerequisiteItems.Add(new TunnelServicePrerequisite(serverConfigurationPrerequisite));
            
            DataContext = Model = mainWindowModel;
        }

        private MainWindowModel Model { get; }
    }
}
