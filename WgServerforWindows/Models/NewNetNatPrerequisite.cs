using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;
using SharpConfig;
using WgAPI;
using WgServerforWindows.Controls;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class NewNetNatPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public NewNetNatPrerequisite() : this(new NewNetIpAddressTaskSubCommand()) { }

        public NewNetNatPrerequisite(NewNetIpAddressTaskSubCommand newNetIpAddressTaskSubCommand) : base
        (
            title: Resources.NewNatName,
            successMessage: Resources.NewNetSuccess,
            errorMessage: Resources.NewNetError,
            resolveText: Resources.NewNatResolve,
            configureText: Resources.NewNatConfigure
        )
        {
            _newNetIpAddressTaskSubCommand = newNetIpAddressTaskSubCommand;
            SubCommands.Add(_newNetIpAddressTaskSubCommand);
        }

        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
        {
            if (!File.Exists(ServerConfigurationPrerequisite.ServerDataPath))
            {
                // The server config doesn't exist yet.
                // We can't even evaluate what the NAT should be.
                return false;
            }

            bool result = true;
            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath));

            // Get the network interface
            int? index = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == ServerConfigurationPrerequisite.WireGuardServerInterfaceName)?
                .GetIPProperties()?.GetIPv4Properties()?.Index;

            // Verify the NAT rule exists and is correct
            string output = new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile Get-NetNat -Name {_netNatName}"),
                out int exitCode);

            result &= exitCode == 0 && output.Contains(serverConfiguration.AddressProperty.Value);

            // Verify the interface's IP address is correct
            output = new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile Get-NetIPAddress -InterfaceIndex {index}"),
                out exitCode);

            result &= exitCode == 0 && output.Contains(serverConfiguration.IpAddress);

            return result;
        });
        private BooleanTimeCachedProperty _fulfilled;

        public override void Resolve()
        {
            Resolve(default);
        }

        public void Resolve(string serverDataPath)
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            // Get the network interface
            int? index = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == ServerConfigurationPrerequisite.WireGuardServerInterfaceName)?
                .GetIPProperties()?.GetIPv4Properties()?.Index;

            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(serverDataPath ?? ServerConfigurationPrerequisite.ServerDataPath));

            // Remove any pre-existing IP addresses on this interface, ignore errors
            new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile Remove-NetIPAddress -InterfaceIndex {index} -Confirm:$false"),
                out int exitCode);

            // Assign the IP address to the interface
            string output = new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile New-NetIPAddress -IPAddress {serverConfiguration.IpAddress} -PrefixLength {serverConfiguration.Subnet} -InterfaceIndex {index}"),
                out exitCode);

            if (exitCode != 0)
            {
                throw new Exception(output);
            }

            // Remove any existing NAT routing rule, ignore errors
            new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile Remove-NetNat -Name {_netNatName} -Confirm:$false"),
                out exitCode);

            // Create the NAT routing rule
            output = new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile New-NetNat -Name {_netNatName} -InternalIPInterfaceAddressPrefix {serverConfiguration.AddressProperty.Value}"),
                out exitCode);

            if (exitCode != 0)
            {
                // Windows is telling us that New-NetNat is unsupported. Ask the user if they want to try enabling Hyper-V.
                var res = MessageBox.Show(Resources.PromptForHyperV, Resources.WS4W, MessageBoxButton.YesNo);

                if (res == MessageBoxResult.Yes)
                {
                    // Let's try to enabled Hyper-V.
                    new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.CustomInteractive,
                            "powershell.exe",
                            "-NoProfile Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All"),
                        out exitCode);

                    if (exitCode == 0)
                    {
                        // Seems to have installed successfully. Prompt for reboot
                        MessageBox.Show(Resources.PromptForHyperVReboot, Resources.WS4W, MessageBoxButton.OK);
                    }
                    else
                    {
                        WaitCursor.SetOverrideCursor(null);

                        // If we get here, the Hyper-V install failed for some reason (e.g., Windows Home). Recommend ICS.
                        new UnhandledErrorWindow
                        {
                            DataContext = new UnhandledErrorWindowModel
                            {
                                Title = Resources.Error,
                                Text = Resources.HyperVErrorNatRoutingNotSupported,
                                Exception = new Exception($"{output}{Environment.StackTrace}")
                            }
                        }.ShowDialog();
                    }
                }
                else
                {
                    WaitCursor.SetOverrideCursor(null);

                    // If we get here, the user chose not to install Hyper-V. Recommend ICS.
                    new UnhandledErrorWindow
                    {
                        DataContext = new UnhandledErrorWindowModel
                        {
                            Title = Resources.Error,
                            Text = Resources.NatRoutingNotSupported,
                            Exception = new Exception($"{output}{Environment.StackTrace}")
                        }
                    }.ShowDialog();
                }
            }
            else
            {
                // If we get here, we know NAT routing succeeded

                // Invoke our subcommand
                _newNetIpAddressTaskSubCommand.Resolve(serverDataPath);
            }

            Refresh();

            WaitCursor.SetOverrideCursor(null);
        }

        public override void Configure()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            // Delete the NAT rule
            new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile Remove-NetNat -Name {_netNatName} -Confirm:$false"),
                out _);

            // Invoke our subcommand
            _newNetIpAddressTaskSubCommand.Configure();

            Refresh();

            WaitCursor.SetOverrideCursor(null);
        }

        public override string Category => Resources.NetworkAddressTranslation;

        #endregion

        #region Public properties

        public bool IsSupported
        {
            get
            {
                new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                        "powershell.exe",
                        "-NoProfile if ((Get-Command New-NetNat).Parameters.ContainsKey('InternalIPInterfaceAddressPrefix')) { exit 0 } else { exit 1 }"),
                    out int exitCode);

                if (exitCode == 0)
                {
                    return true;
                }

                // In case the Get-Command fails, fall back to the old check: Get-Help

                new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                        "powershell.exe",
                        "-NoProfile Get-Help New-NetNat -Parameter InternalIPInterfaceAddressPrefix"),
                    out exitCode);

                return exitCode == 0;
            }
        }

        #endregion

        #region Private readonly

        private readonly string _netNatName = "wg_server_nat";

        private readonly NewNetIpAddressTaskSubCommand _newNetIpAddressTaskSubCommand;

        #endregion
    }
}
