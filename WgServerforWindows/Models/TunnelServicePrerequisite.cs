using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Input;
using WgAPI;
using WgAPI.Commands;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
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

        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Any(nic => nic.Name == ServerConfigurationPrerequisite.WireGuardServerInterfaceName);
        });
        private BooleanTimeCachedProperty _fulfilled;

        public override async void Resolve()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);
            
            new WireGuardExe().ExecuteCommand(new InstallTunnelServiceCommand(ServerConfigurationPrerequisite.ServerWGPath));
            await WaitForFulfilled();
            
            WaitCursor.SetOverrideCursor(null);
        }

        public override async void Configure()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);
            
            new WireGuardExe().ExecuteCommand(new UninstallTunnelServiceCommand(ServerConfigurationPrerequisite.WireGuardServerInterfaceName));
            await WaitForFulfilled(false);
            
            WaitCursor.SetOverrideCursor(null);
        }
    }
}
