using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Input;
using WireGuardAPI;
using WireGuardAPI.Commands;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class TunnelServicePrerequisite : PrerequisiteItem
    {
        public TunnelServicePrerequisite(ServerConfigurationPrerequisite serverConfiguration) : base
        (
            title: Resources.TunnelService,
            successMessage: Resources.TunnelServiceInstalled,
            errorMessage: Resources.TunnelServiceNotInstalled,
            resolveText: Resources.InstallTunnelService,
            configureText: Resources.UninstallTunnelService
        )
        {
            _serverConfigurationPrerequisite = serverConfiguration;
        }

        private readonly ServerConfigurationPrerequisite _serverConfigurationPrerequisite;

        public override bool Fulfilled => NetworkInterface.GetAllNetworkInterfaces()
            .Any(nic => nic.Name == _interfaceName);

        public override void Resolve()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            
            new WireGuardExe().ExecuteCommand(new InstallTunnelServiceCommand(_serverConfigurationPrerequisite.ConfigurationPath));
            Task.Run(WaitForFulfilled);
            
            Mouse.OverrideCursor = null;
        }

        public override void Configure()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            
            new WireGuardExe().ExecuteCommand(new UninstallTunnelServiceCommand(_interfaceName));
            Refresh();
            
            Mouse.OverrideCursor = null;
        }

        #region Private properties

        private string _interfaceName => Path.GetFileNameWithoutExtension(_serverConfigurationPrerequisite.ConfigurationPath);

        #endregion
    }
}
