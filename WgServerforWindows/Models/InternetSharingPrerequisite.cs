using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Windows.Input;
using Humanizer;
using NETCONLib;
using WgServerforWindows.Controls;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
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

        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
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
        });
        private BooleanTimeCachedProperty _fulfilled;

        public override void Resolve()
        {
            Resolve(default);
        }

        public void Resolve(string networkToShare)
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            NetSharingManagerClass netSharingManager = new NetSharingManagerClass();
            
            // Disable sharing wherever it may be enabled first
            foreach (var oldConnection in netSharingManager.EnumEveryConnection
                .OfType<INetConnection>()
                .Where(n => netSharingManager.INetSharingConfigurationForINetConnection[n].SharingEnabled)
                .Select(n => netSharingManager.INetSharingConfigurationForINetConnection[n]))
            {
                oldConnection.DisableSharing();
            }

            // Occasionally have to poke networks via WMI to really disable all ICS
            // - https://github.com/micahmo/WireGuardServerForWindows/issues/16
            // - https://github.com/utapyngo/icsmanager/issues/17
            try
            {
                ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(@"root\Microsoft\HomeNet", "select * from hnet_connectionproperties");
                foreach (ManagementObject netConnection in managementObjectSearcher.Get().OfType<ManagementObject>())
                {
                    if (netConnection.GetPropertyValue("IsIcsPrivate") is bool isIcsPrivate && isIcsPrivate)
                    {
                        netConnection.SetPropertyValue("IsIcsPrivate", false);
                        netConnection.Put(new PutOptions { Type = PutType.UpdateOnly });
                    }

                    if (netConnection.GetPropertyValue("IsIcsPublic") is bool isIcsPublic && isIcsPublic)
                    {
                        netConnection.SetPropertyValue("IsIcsPublic", false);
                        netConnection.Put(new PutOptions { Type = PutType.UpdateOnly });
                    }
                }
            }
            catch (PlatformNotSupportedException)
            {
                throw new Exception("WS4W was unable to enable Internet Sharing due to an old version of .NET Framework. Please apply all Windows Updates before continuing, and then try again.");
            }

            WaitCursor.SetOverrideCursor(null);

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

                // Get the IP address assigned to the adapter, if any. Prefer IPv4.
                string address = default;
                if (NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Id == netSharingManager.NetConnectionProps[connection].Guid)?.GetIPProperties() is { } ipProperties)
                {
                    address = (ipProperties.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                               ?? ipProperties.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6))?.Address.ToString();
                }

                selectionWindowModel.Items.Add(new SelectionItem<INetConnection>
                {
                    DisplayText = $"{netSharingManager.NetConnectionProps[connection].Name} ({status})",
                    Description = $"{netSharingManager.NetConnectionProps[connection].DeviceName}{(string.IsNullOrEmpty(address) ? string.Empty : $" ({address})")}",
                    BackingObject = connection
                });
            }

            INetConnection internetConnection;
            if (string.IsNullOrEmpty(networkToShare))
            {
                // No network given, prompt for selection.
                new SelectionWindow { DataContext = selectionWindowModel }.ShowDialog();
                internetConnection = selectionWindowModel.DialogResult == true ? selectionWindowModel.SelectedItem?.BackingObject : default;
            }
            else
            {
                // Find the network matching the given name.
                internetConnection = netSharingManager.EnumEveryConnection.OfType<INetConnection>().FirstOrDefault(n => netSharingManager.NetConnectionProps[n].Name.Equals(networkToShare, StringComparison.OrdinalIgnoreCase));
            }
            
            if (internetConnection is { })
            {
                WaitCursor.SetOverrideCursor(Cursors.Wait);

                var wg_server = netSharingManager.EnumEveryConnection.OfType<INetConnection>().FirstOrDefault(n => netSharingManager.NetConnectionProps[n].Name == ServerConfigurationPrerequisite.WireGuardServerInterfaceName);

                if (wg_server is { })
                {
                    netSharingManager.INetSharingConfigurationForINetConnection[internetConnection].EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC);
                    netSharingManager.INetSharingConfigurationForINetConnection[wg_server].EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE);
                }

                Refresh();

                WaitCursor.SetOverrideCursor(null);
            }

            Refresh();
        }

        public override void Configure()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            NetSharingManagerClass netSharingManager = new NetSharingManagerClass();

            foreach (var oldConnection in netSharingManager.EnumEveryConnection
                .OfType<INetConnection>()
                .Where(n => netSharingManager.INetSharingConfigurationForINetConnection[n].SharingEnabled)
                .Select(n => netSharingManager.INetSharingConfigurationForINetConnection[n]))
            {
                oldConnection.DisableSharing();
            }

            Refresh();

            WaitCursor.SetOverrideCursor(null);
        }

        /// <summary>
        /// Returns the network(s) (if any) that is/are currently being shared.
        /// </summary>
        public List<string> GetSharedNetworks()
        {
            List<string> result = new List<string>();
            
            NetSharingManagerClass netSharingManager = new NetSharingManagerClass();

            foreach (var connection in netSharingManager.EnumEveryConnection.OfType<INetConnection>().Where(c => netSharingManager.NetConnectionProps[c].Name != ServerConfigurationPrerequisite.WireGuardServerInterfaceName))
            {
                if (netSharingManager.INetSharingConfigurationForINetConnection[connection].SharingEnabled &&
                    netSharingManager.INetSharingConfigurationForINetConnection[connection].SharingConnectionType == tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC)
                {
                    result.Add(netSharingManager.NetConnectionProps[connection].Name);
                }
            }

            return result;
        }

        public override string Category => Resources.InternetConnectionSharing;

        #endregion
    }
}
