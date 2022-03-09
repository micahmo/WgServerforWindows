using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Input;
using SharpConfig;
using WireGuardAPI;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class NewNetNatPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public NewNetNatPrerequisite() : base
        (
            title: Resources.NewNatName,
            successMessage: Resources.NewNetSuccess,
            errorMessage: Resources.NewNetError,
            resolveText: Resources.NewNatResolve,
            configureText: Resources.NewNatConfigure
        )
        {
        }

        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
        {
            bool result = true;
            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath));

            // Get the network interface
            int? index = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == ServerConfigurationPrerequisite.WireGuardServerInterfaceName)?
                .GetIPProperties()?.GetIPv4Properties()?.Index;

            string output = new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile Get-NetNat -Name {_netNatName}"),
                out int exitCode);

            result &= exitCode == 0 && output.Contains(serverConfiguration.AddressProperty.Value);

            output = new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile Get-NetIpAddress -InterfaceIndex {index}"),
                out exitCode);

            result &= exitCode == 0 && output.Contains(serverConfiguration.IpAddress);

            return result;
        });
        private BooleanTimeCachedProperty _fulfilled;

        public override void Resolve()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            // Get the network interface
            int? index = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == ServerConfigurationPrerequisite.WireGuardServerInterfaceName)?
                .GetIPProperties()?.GetIPv4Properties()?.Index;

            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath));

            // Remove any pre-existing IP addresses on this interface, ignore errors
            new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile Remove-NetIpAddress -InterfaceIndex {index} -Confirm:$false"),
                out int exitCode);

            // Assign the IP address to the interface
            string output = new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    //$"-NoProfile New-NetIPAddress -IPAddress {serverConfiguration.IpAddress} -PrefixLength {serverConfiguration.Subnet} -InterfaceIndex {index} -PolicyStore PersistentStore"),
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
                throw new Exception(output);
            }

            Refresh();

            Mouse.OverrideCursor = null;
        }

        public override void Configure()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            new WireGuardExe().ExecuteCommand(new WireGuardCommand(string.Empty, WhichExe.Custom,
                    "powershell.exe",
                    $"-NoProfile Remove-NetNat -Name {_netNatName} -Confirm:$false"),
                out _);

            Refresh();

            Mouse.OverrideCursor = null;
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
                        "-NoProfile Get-Command New-NetNat"),
                    out int exitCode);

                return exitCode == 0;
            }
        }

        #endregion

        #region Private readonly

        private readonly string _netNatName = "wg_server_nat";

        #endregion
    }
}
