using System.Linq;
using System.Windows.Input;
using NETCONLib;
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

        // ----------------- TODO -----------------
        // Support selecting public interface to share!!
        // Find/replace "Ethernet"
        // ----------------- TODO -----------------

        public override bool Fulfilled
        {
            get
            {
                bool result = false;

                NetSharingManagerClass netSharingManager = new NetSharingManagerClass();
                var internetConnection = netSharingManager.EnumEveryConnection.OfType<INetConnection>().FirstOrDefault(n => netSharingManager.NetConnectionProps[n].Name == "Ethernet");

                if (internetConnection is { })
                {
                    result = netSharingManager.INetSharingConfigurationForINetConnection[internetConnection].SharingEnabled;
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
            
            var internetConnection = netSharingManager.EnumEveryConnection.OfType<INetConnection>().FirstOrDefault(n => netSharingManager.NetConnectionProps[n].Name == "Ethernet");
            var wg_server = netSharingManager.EnumEveryConnection.OfType<INetConnection>().FirstOrDefault(n => netSharingManager.NetConnectionProps[n].Name == ServerConfigurationPrerequisite.WireGuardServerInterfaceName);

            if (internetConnection is { } && wg_server is { })
            {
                netSharingManager.INetSharingConfigurationForINetConnection[internetConnection].EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC);
                netSharingManager.INetSharingConfigurationForINetConnection[wg_server].EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE);
            }

            Refresh();

            Mouse.OverrideCursor = null;
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
