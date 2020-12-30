using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Net;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class PrivateNetworkPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public PrivateNetworkPrerequisite() : base
        (
            title: Resources.PrivateNetworkTitle,
            successMessage: Resources.PrivateNetworkSuccess,
            errorMessage: Resources.PrivateNetworkError,
            resolveText: Resources.PrivateNetworkResolve,
            configureText: Resources.PrivateNetworkConfigure
        )
        {
        }

        public override bool Fulfilled
        {
            get
            {
                bool result = false;

                if (GetNetwork() is { } network)
                {
                    result = network.Category == NetworkCategory.Private;
                }

                return result;
            }
        }

        public override void Resolve()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (GetNetwork() is { } network)
            {
                network.Category = NetworkCategory.Private;
            }

            Refresh();

            Mouse.OverrideCursor = null;
        }

        public override void Configure()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (GetNetwork() is { } network)
            {
                network.Category = NetworkCategory.Public;
            }

            Refresh();

            Mouse.OverrideCursor = null;
        }

        #endregion

        #region Private methods

        private Network GetNetwork()
        {
            Network result = default;

            // Windows API code pack can show stale adapters, and incorrect names.
            // First, get the real interface here.
            if (NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == ServerConfigurationPrerequisite.WireGuardServerInterfaceName) is { } networkInterface)
            {
                // Now use the ID to get the network from API code pack
                if (NetworkListManager.GetNetworks(NetworkConnectivityLevels.All).FirstOrDefault(n => n.Connections.Any(c => c.AdapterId == new Guid(networkInterface.Id))) is { } network)
                {
                    result = network;
                }
            }

            return result;
        }

        #endregion
    }
}
