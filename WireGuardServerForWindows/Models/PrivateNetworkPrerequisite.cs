using System;
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

        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
        {
            bool result = false;

            // Check whether the Tunnel service is installed. This will inform whether we should wait a long time to find the network or not
            var tun = new TunnelServicePrerequisite().Fulfilled;
            TimeSpan timeout = TimeSpan.FromSeconds(tun ? 10 : 0);

            if (ServerConfigurationPrerequisite.GetNetwork(timeout: timeout) is { } network)
            {
                // Special case: computer is on a domain, so Authenticated is sufficient and shouldn't be changed
                if (network.Category == NetworkCategory.Authenticated)
                {
                    SuccessMessage = Resources.WireGuardNetworkOnDomain;
                    _isInformational = true;
                }
                else
                {
                    SuccessMessage = Resources.PrivateNetworkSuccess;
                    _isInformational = false;
                }

                RaisePropertyChanged(nameof(CanConfigure));
                RaisePropertyChanged(nameof(CanResolve));

                // Normal case: We want the network to be private
                result = network.Category == NetworkCategory.Private;
            }

            return result;
        });
        private BooleanTimeCachedProperty _fulfilled;

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
                try
                {
                    network.Category = NetworkCategory.Public;
                }
                catch (UnauthorizedAccessException)
                {
                    // If it failed, maybe we're on a domain?
                    if (network.Category == NetworkCategory.Authenticated)
                    {
                        // Just keep going. Refresh() will raise Fulfilled, which will check the category agian
                    }
                    else // Failed for some other reason. Let it fail.
                    {
                        throw;
                    }
                }
            }

            Refresh();

            Mouse.OverrideCursor = null;
        }

        public override BooleanTimeCachedProperty IsInformational => _isInformationalProperty ??= new BooleanTimeCachedProperty(TimeSpan.Zero, () => _isInformational);
        private BooleanTimeCachedProperty _isInformationalProperty;
        private bool _isInformational;

        #endregion
    }
}
