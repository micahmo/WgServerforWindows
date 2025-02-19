using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Bluegrams.Application;
using Bluegrams.Application.WPF;
using SharpConfig;
using WgServerforWindows.Models;
using SplashScreen = WgServerforWindows.Controls.SplashScreen;

namespace WgServerforWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            AppSettings.Instance.Load();

            InitializeComponent();

            // Never put quotes around config file values
            Configuration.OutputRawStringValues = true;

            var wireGuardExePrerequisite = new WireGuardExePrerequisite();
            var openServerConfigDirectorySubCommand = new OpenServerConfigDirectorySubCommand();
            var changeServerConfigDirectorySubCommand = new ChangeServerConfigDirectorySubCommand();
            var serverConfigurationPrerequisite = new ServerConfigurationPrerequisite(openServerConfigDirectorySubCommand, changeServerConfigDirectorySubCommand);
            var openClientConfigDirectorySubCommand = new OpenClientConfigDirectorySubCommand();
            var changeClientConfigDirectorySubCommand = new ChangeClientConfigDirectorySubCommand();
            var clientConfigurationsPrerequisite = new ClientConfigurationsPrerequisite(openClientConfigDirectorySubCommand, changeClientConfigDirectorySubCommand);
            var tunnelServiceNameSubCommand = new TunnelServiceNameSubCommand();
            var tunnelServicePrerequisite = new TunnelServicePrerequisite(tunnelServiceNameSubCommand);
            var privateNetworkTaskSubCommand = new PrivateNetworkTaskSubCommand();
            var privateNetworkPrerequisite = new PrivateNetworkPrerequisite(privateNetworkTaskSubCommand);
            var netIpAddressTaskSubCommand = new NewNetIpAddressTaskSubCommand();
            var netNatRangeSubCommand = new NetNatRangeSubCommand();
            var newNetNatPrerequisite = new NewNetNatPrerequisite(netIpAddressTaskSubCommand, netNatRangeSubCommand);
            var internetSharingPrerequisite = new InternetSharingPrerequisite();
            var persistentInternetSharingPrerequisite = new PersistentInternetSharingPrerequisite();
            var serverStatusPrerequisite = new ServerStatusPrerequisite();
            var bootTaskDelaySubCommand = new BootTaskDelaySubCommand();
            var settingsPrerequisite = new SettingsPrerequisite(bootTaskDelaySubCommand);

            // -- Set up interdependencies --

            // Can't uninstall WireGuard while Tunnel is installed
            wireGuardExePrerequisite.CanConfigureFunc = () => tunnelServicePrerequisite.Fulfilled == false;

            // Can't resolve or configure server or client unless WireGuard is installed
            serverConfigurationPrerequisite.CanResolveFunc = clientConfigurationsPrerequisite.CanResolveFunc =
            serverConfigurationPrerequisite.CanConfigureFunc = clientConfigurationsPrerequisite.CanConfigureFunc = () => wireGuardExePrerequisite.Fulfilled;
            
            // Can't rename the tunnel service if it's already installed
            tunnelServiceNameSubCommand.CanConfigureFunc = () => tunnelServicePrerequisite.Fulfilled == false;

            // Can't install tunnel until WireGuard exe is installed and server is configured
            tunnelServicePrerequisite.CanResolveFunc = () =>
                wireGuardExePrerequisite.Fulfilled && serverConfigurationPrerequisite.Fulfilled;

            // Can't uninstall the tunnel while internet sharing is enabled
            tunnelServicePrerequisite.CanConfigureFunc = () => internetSharingPrerequisite.Fulfilled == false && newNetNatPrerequisite.Fulfilled == false;
            
            // Can't enable private network unless tunnel is installed, and private network must not be informational
            privateNetworkPrerequisite.CanResolveFunc = () => tunnelServicePrerequisite.Fulfilled &&
                                                              privateNetworkPrerequisite.IsInformational == false;

            // Can't configure private network if it's only information (e.g., on a domain)
            privateNetworkPrerequisite.CanConfigureFunc = () => privateNetworkPrerequisite.IsInformational == false;

            // Can't enable/disable automatic private network if it's not already enabled.
            privateNetworkTaskSubCommand.CanResolveFunc = privateNetworkTaskSubCommand.CanConfigureFunc = () => privateNetworkPrerequisite.Fulfilled;

            // Can't enable internet sharing unless tunnel is installed
            internetSharingPrerequisite.CanResolveFunc = () => tunnelServicePrerequisite.Fulfilled;

            // Can't view server status unless tunnel is installed
            serverStatusPrerequisite.CanConfigureFunc = () => tunnelServicePrerequisite.Fulfilled;

            // Can't open server or folders unless they exist
            openServerConfigDirectorySubCommand.CanConfigureFunc = () => Directory.Exists(ServerConfigurationPrerequisite.ServerConfigDirectory);
            openClientConfigDirectorySubCommand.CanConfigureFunc = () => Directory.Exists(ClientConfigurationsPrerequisite.ClientConfigDirectory);

            // Add the prereqs to the Model
            MainWindowModel mainWindowModel = new MainWindowModel();
            mainWindowModel.PrerequisiteItems.Add(wireGuardExePrerequisite);
            mainWindowModel.PrerequisiteItems.Add(serverConfigurationPrerequisite);
            mainWindowModel.PrerequisiteItems.Add(clientConfigurationsPrerequisite);
            mainWindowModel.PrerequisiteItems.Add(tunnelServicePrerequisite);
            mainWindowModel.PrerequisiteItems.Add(privateNetworkPrerequisite);

            if (newNetNatPrerequisite.IsSupported)
            {
                internetSharingPrerequisite.CanResolveFunc = () => tunnelServicePrerequisite.Fulfilled && !newNetNatPrerequisite.Fulfilled;
                persistentInternetSharingPrerequisite.CanResolveFunc = () => !newNetNatPrerequisite.Fulfilled;
                newNetNatPrerequisite.CanResolveFunc = () => serverConfigurationPrerequisite.Fulfilled
                                                             && tunnelServicePrerequisite.Fulfilled
                                                             && !internetSharingPrerequisite.Fulfilled
                                                             && !persistentInternetSharingPrerequisite.Fulfilled;

                netIpAddressTaskSubCommand.CanResolveFunc = netIpAddressTaskSubCommand.CanConfigureFunc = () => newNetNatPrerequisite.Fulfilled;
                netNatRangeSubCommand.CanConfigureFunc = () => serverConfigurationPrerequisite.Fulfilled;

                var natPrerequisiteGroup = new NatPrerequisiteGroup(newNetNatPrerequisite, internetSharingPrerequisite, persistentInternetSharingPrerequisite);

                if (internetSharingPrerequisite.Fulfilled || persistentInternetSharingPrerequisite.Fulfilled)
                {
                    natPrerequisiteGroup.SelectedChildIndex = 1;
                }

                mainWindowModel.PrerequisiteItems.Add(natPrerequisiteGroup);
            }
            else
            {
                mainWindowModel.PrerequisiteItems.Add(internetSharingPrerequisite);
                mainWindowModel.PrerequisiteItems.Add(persistentInternetSharingPrerequisite);
            }

            mainWindowModel.PrerequisiteItems.Add(serverStatusPrerequisite);
            mainWindowModel.PrerequisiteItems.Add(settingsPrerequisite);

            // If one of the prereqs changes, check the validity of all of them.
            // Do this recursively.
            void AddPrerequisiteItemFulfilledChangedHandler(PrerequisiteItem prerequisiteItem)
            {
                prerequisiteItem.PropertyChanged += PrerequisiteItemFulfilledChanged;
                prerequisiteItem.Children.ToList().ForEach(AddPrerequisiteItemFulfilledChangedHandler);
                prerequisiteItem.SubCommands.ToList().ForEach(AddPrerequisiteItemFulfilledChangedHandler);
            }
            mainWindowModel.PrerequisiteItems.ForEach(AddPrerequisiteItemFulfilledChangedHandler);

            void RemovePrerequisiteItemFulfilledChangedHandler(PrerequisiteItem prerequisiteItem)
            {
                prerequisiteItem.PropertyChanged -= PrerequisiteItemFulfilledChanged;
                prerequisiteItem.Children.ToList().ForEach(RemovePrerequisiteItemFulfilledChangedHandler);
                prerequisiteItem.SubCommands.ToList().ForEach(RemovePrerequisiteItemFulfilledChangedHandler);
            }

            void PrerequisiteItemFulfilledChanged(object sender, PropertyChangedEventArgs e)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Unsubscribe before invoking on everyone
                    mainWindowModel.PrerequisiteItems.ForEach(RemovePrerequisiteItemFulfilledChangedHandler);

                    WaitCursor.SetOverrideCursor(Cursors.Wait);

                    if (sender is PrerequisiteItem senderItem && e.PropertyName == nameof(PrerequisiteItem.Fulfilled))
                    {
                        // Now invoke on all but the sender
                        mainWindowModel.PrerequisiteItems.Where(i => i != senderItem).ToList().ForEach(prerequisiteItem =>
                        {
                            void RaisePropertiesChanged(PrerequisiteItem i)
                            {
                                i.RaisePropertyChanged(nameof(i.Fulfilled));
                                i.RaisePropertyChanged(nameof(i.IsInformational));
                                i.RaisePropertyChanged(nameof(i.CanConfigure));
                                i.RaisePropertyChanged(nameof(i.CanResolve));

                                i.Children.Where(i2 => i2 != senderItem).ToList().ForEach(RaisePropertiesChanged);
                                i.SubCommands.Where(i2 => i2 != senderItem).ToList().ForEach(RaisePropertiesChanged);
                            }

                            RaisePropertiesChanged(prerequisiteItem);
                        });
                    }

                    WaitCursor.SetOverrideCursor(null);

                    // Now we can resubscribe to all
                    mainWindowModel.PrerequisiteItems.ForEach(AddPrerequisiteItemFulfilledChangedHandler);
                });
            }

            DataContext = mainWindowModel;

            // Check for updates
            _updateChecker = new MyUpdateChecker("https://raw.githubusercontent.com/micahmo/WgServerforWindows/master/WireGuardServerForWindows/VersionInfo2.xml", this);
        }

        #region Private fields

        private readonly WpfUpdateChecker _updateChecker;

        #endregion

        #region Event handlers

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            AppSettings.Instance.Tracker.Track(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.Windows.OfType<SplashScreen>().ToList().ForEach(w => w.Close());
            WaitCursor.IgnoreOverrideCursor = false;
            WaitCursor.SetOverrideCursor(null);

            // Auto allows the user to Skip (updates are still available via F1)
            _updateChecker.CheckForUpdates(UpdateNotifyMode.Auto);
        }

        #endregion

        private void AboutBoxCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            new AboutBox(Icon, showLanguageSelection: false)
            {
                Owner = this,
                UpdateChecker = _updateChecker,
            }.ShowDialog();
        }

        private void RefreshCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Raise Fulfilled PropertyChanged on any prerequisite item. This will trigger the rest to update as well.
            (DataContext as MainWindowModel)?.PrerequisiteItems.FirstOrDefault()?.RaisePropertyChanged(nameof(PrerequisiteItem.Fulfilled));
        }
    }
}
