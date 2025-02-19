using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Input;
using WgAPI;
using WgAPI.Commands;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class TunnelServicePrerequisite : PrerequisiteItem
    {
        public TunnelServicePrerequisite() : this(new TunnelServiceNameSubCommand())
        {
        }

        public TunnelServicePrerequisite(TunnelServiceNameSubCommand tunnelServiceNameSubCommand) : base
        (
            title: Resources.TunnelService,
            successMessage: Resources.TunnelServiceInstalled,
            errorMessage: Resources.TunnelServiceNotInstalled,
            resolveText: Resources.InstallTunnelService,
            configureText: Resources.UninstallTunnelService
        )
        {
            SubCommands.Add(tunnelServiceNameSubCommand);
        }

        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Any(nic => nic.Name == GlobalAppSettings.Instance.TunnelServiceName);
        });
        private BooleanTimeCachedProperty _fulfilled;

        public override async void Resolve()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            using (TemporaryFile temporaryFile = new(ServerConfigurationPrerequisite.ServerWGPath, ServerConfigurationPrerequisite.ServerWGPathWithCustomTunnelName))
            {
                new WireGuardExe().ExecuteCommand(new InstallTunnelServiceCommand(temporaryFile.NewFilePath));
            }
            
            await WaitForFulfilled();
            
            WaitCursor.SetOverrideCursor(null);
        }

        public override async void Configure()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            new WireGuardExe().ExecuteCommand(new UninstallTunnelServiceCommand(GlobalAppSettings.Instance.TunnelServiceName));
            await WaitForFulfilled(false);

            WaitCursor.SetOverrideCursor(null);
        }
    }
}
