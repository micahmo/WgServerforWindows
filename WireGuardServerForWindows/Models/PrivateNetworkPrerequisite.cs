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

                if (ServerConfigurationPrerequisite.GetNetwork() is { } network)
                {
                    result = network.Category == NetworkCategory.Private;
                }

                return result;
            }
        }

        public override void Resolve()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (ServerConfigurationPrerequisite.GetNetwork() is { } network)
            {
                network.Category = NetworkCategory.Private;
            }

            Refresh();

            Mouse.OverrideCursor = null;
        }

        public override void Configure()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (ServerConfigurationPrerequisite.GetNetwork() is { } network)
            {
                network.Category = NetworkCategory.Public;
            }

            Refresh();

            Mouse.OverrideCursor = null;
        }

        #endregion
    }
}
