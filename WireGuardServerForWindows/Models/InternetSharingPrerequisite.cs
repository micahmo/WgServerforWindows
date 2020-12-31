using System.Linq;
using System.Windows.Input;
using Humanizer;
using NETCONLib;
using WireGuardServerForWindows.Controls;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class InternetSharingPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public InternetSharingPrerequisite() : base
        (
            title: Resources.InternetSharingTitle,
            successMessage: Resources.InternetSharingSuccess,
            errorMessage: Resources.InternetSharingError,
            resolveText: Resources.InternetSharingResolve,
            configureText: Resources.InternetSharingConfigure
        )
        {
        }

        public override bool Fulfilled
        {
            get
            {
                bool result = false;

                NetSharingManagerClass netSharingManager = new NetSharingManagerClass();
                
                // Find the WireGuard interface
                var wg_server = netSharingManager.EnumEveryConnection.OfType<INetConnection>()
                    .FirstOrDefault(n => netSharingManager.NetConnectionProps[n].Name == ServerConfigurationPrerequisite.WireGuardServerInterfaceName);

                if (wg_server is { })
                {
                    result = netSharingManager.INetSharingConfigurationForINetConnection[wg_server].SharingEnabled &&
                             netSharingManager.INetSharingConfigurationForINetConnection[wg_server].SharingConnectionType == tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE;
                }

                return result;
            }
        }

        public override void Resolve()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            NetSharingManagerClass netSharingManager = new NetSharingManagerClass();
            
            // Disable sharing wherever it may be enabled first
            foreach (var oldConnection in netSharingManager.EnumEveryConnection
                .OfType<INetConnection>()
                .Where(n => netSharingManager.INetSharingConfigurationForINetConnection[n].SharingEnabled)
                .Select(n => netSharingManager.INetSharingConfigurationForINetConnection[n]))
            {
                oldConnection.DisableSharing();
            }

            Mouse.OverrideCursor = null;

            // Allow the user to pick the interface to share
            var selectionWindowModel = new SelectionWindowModel<INetConnection>
            {
                Title = Resources.SelectInterfaceTitle,
                Text = Resources.SelectInterfaceText,
            };

            // Add all of the interfaces to the selection list
            foreach (var connection in netSharingManager.EnumEveryConnection.OfType<INetConnection>().Where(c => netSharingManager.NetConnectionProps[c].Name != ServerConfigurationPrerequisite.WireGuardServerInterfaceName))
            {
                // Status is an enum like NCS_MEDIA_DISCONNECTED
                // Humanize() will convert it to "NCS MEDIA DISCONNECTED"
                // Transform(To.LowerCase, To.TitleCase) will convert it to "Ncs Media Disconnected"
                // Split will split it into "Ncs" "Media" "Disconnected"
                // Skip(1) will remove the "Ncs" and result in "Media" "Disconnected"
                // string.Join(' ', ...) will put it back together like "Media Disconnected"
                string status = string.Join(' ', netSharingManager.NetConnectionProps[connection].Status.Humanize().Transform(To.LowerCase, To.TitleCase).Split().Skip(1));

                selectionWindowModel.Items.Add(new SelectionItem<INetConnection>
                {
                    DisplayText = $"{netSharingManager.NetConnectionProps[connection].Name} ({status})",
                    Description = netSharingManager.NetConnectionProps[connection].DeviceName,
                    BackingObject = connection
                });
            }

            new SelectionWindow {DataContext = selectionWindowModel}.ShowDialog();
            if (selectionWindowModel.DialogResult == true && selectionWindowModel.SelectedItem?.BackingObject is { } internetConnection)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var wg_server = netSharingManager.EnumEveryConnection.OfType<INetConnection>().FirstOrDefault(n => netSharingManager.NetConnectionProps[n].Name == ServerConfigurationPrerequisite.WireGuardServerInterfaceName);

                if (wg_server is { })
                {
                    netSharingManager.INetSharingConfigurationForINetConnection[internetConnection].EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC);
                    netSharingManager.INetSharingConfigurationForINetConnection[wg_server].EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE);
                }

                Refresh();

                Mouse.OverrideCursor = null;
            }

            Refresh();
        }

        public override void Configure()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            NetSharingManagerClass netSharingManager = new NetSharingManagerClass();

            foreach (var oldConnection in netSharingManager.EnumEveryConnection
                .OfType<INetConnection>()
                .Where(n => netSharingManager.INetSharingConfigurationForINetConnection[n].SharingEnabled)
                .Select(n => netSharingManager.INetSharingConfigurationForINetConnection[n]))
            {
                oldConnection.DisableSharing();
            }

            Refresh();

            Mouse.OverrideCursor = null;
        }

        #endregion
    }
}
