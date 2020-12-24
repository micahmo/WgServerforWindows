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
            mainWindowModel.PrerequisiteItems.Add(new ServerConfigurationPrerequisite());
            mainWindowModel.PrerequisiteItems.Add(new ClientConfigurationsPrerequisite());
            mainWindowModel.PrerequisiteItems.Add(new TunnelServicePrerequisite());

            DataContext = Model = mainWindowModel;
        }

        private MainWindowModel Model { get; }
    }
}
