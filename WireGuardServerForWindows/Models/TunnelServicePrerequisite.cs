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
        public TunnelServicePrerequisite() : base
        (
            title: Resources.TunnelService,
            successMessage: Resources.TunnelServiceInstalled,
            errorMessage: Resources.TunnelServiceNotInstalled,
            resolveText: Resources.InstallTunnelService,
            configureText: Resources.UninstallTunnelService
        )
        {
        }

        public override bool Fulfilled => NetworkInterface.GetAllNetworkInterfaces()
            .Any(nic => nic.Name == Path.GetFileNameWithoutExtension(ServerConfigurationPrerequisite.ServerWGPath));

        public override void Resolve()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            
            new WireGuardExe().ExecuteCommand(new InstallTunnelServiceCommand(ServerConfigurationPrerequisite.ServerWGPath));
            Task.Run(WaitForFulfilled);
            
            Mouse.OverrideCursor = null;
        }

        public override void Configure()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            
            new WireGuardExe().ExecuteCommand(new UninstallTunnelServiceCommand(Path.GetFileNameWithoutExtension(ServerConfigurationPrerequisite.ServerWGPath)));
            Refresh();
            
            Mouse.OverrideCursor = null;
        }
    }
}
