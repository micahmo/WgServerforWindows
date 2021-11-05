using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Net;
using SharpConfig;
using WireGuardAPI;
using WireGuardAPI.Commands;
using WireGuardServerForWindows.Controls;
using WireGuardServerForWindows.Extensions;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ServerConfigurationPrerequisite : PrerequisiteItem
    {
        #region Constructor

        public ServerConfigurationPrerequisite() : base
        (
            title: Resources.ServerConfiguration,
            successMessage: Resources.ServerConfigurationSuccessMessage,
            errorMessage: Resources.ServerConfigurationMissingErrorMessage,
            resolveText: Resources.ServerConfigurationConfigureText,
            configureText: Resources.ServerConfigurationConfigureText
        ) { }

        #endregion

        #region PrerequisiteItem members

        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
        {
            if (File.Exists(ServerWGPath) == false)
            {
                ErrorMessage = Resources.ServerConfigurationMissingErrorMessage;
                return false;
            }
            
            
            // The file exists, make sure it has all the fields
            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerDataPath));

            foreach (ConfigurationProperty property in serverConfiguration.Properties)
            {
                if (string.IsNullOrEmpty(property.Validation?.Validate?.Invoke(property)) == false)
                {
                    ErrorMessage = Resources.ServerConfigurationIncompleteErrorMessage;
                    return false;
                }
            }

            // Check whether the registry got updated correctly.
            if (!GetScopeAddressRegistryValue().Equals(serverConfiguration.IpAddress))
            {
                ErrorMessage = Resources.ScopeAddressRegistryIncorrect;
                return false;
            }

            // If we get here, everything passed.
            return true;
        });
        private BooleanTimeCachedProperty _fulfilled;

        public override void Resolve()
        {
            if (Directory.Exists(Path.GetDirectoryName(ServerDataPath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ServerDataPath));
            }

            if (File.Exists(ServerDataPath) == false)
            {
#pragma warning disable CS0642
                // There is intentionally no code block after the using statement,
                // because we want to create and then release the file without holding it open.
                using (File.Create(ServerDataPath));
#pragma warning restore CS0642
            }

            if (Directory.Exists(Path.GetDirectoryName(ServerWGPath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ServerWGPath));
            }

            if (File.Exists(ServerWGPath) == false)
            {
#pragma warning disable CS0642
                // There is intentionally no code block after the using statement,
                // because we want to create and then release the file without holding it open.
                using (File.Create(ServerWGPath));
#pragma warning restore CS0642
            }

            Configure();
        }

        public override void Configure()
        {
            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerDataPath));
            string originalServerIp = serverConfiguration.AddressProperty.Value;
            
            ServerConfigurationEditorWindow serverConfigurationEditor = new ServerConfigurationEditorWindow {DataContext = serverConfiguration};

            Mouse.OverrideCursor = Cursors.Wait;
            if (serverConfigurationEditor.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Save to Data
                SaveData(serverConfiguration);

                // Save to WG
                SaveWG(serverConfiguration);

                // Update clients
                var clientConfigurationsPrerequisite = new ClientConfigurationsPrerequisite();
                clientConfigurationsPrerequisite.Update();

                // Update Internet Sharing to use new server IP only if
                // - the new value passes validation
                // - the new value is not already in the registry
                if (string.IsNullOrEmpty(serverConfiguration.AddressProperty.Validation?.Validate?.Invoke(serverConfiguration.AddressProperty))
                    && !GetScopeAddressRegistryValue().Equals(serverConfiguration.IpAddress))
                {
                    SetScopeAddressRegistryValue(serverConfiguration.IpAddress);

                    // If Internet Sharing is already enabled, and we just changed the server's network range, we should disable and re-enable ICS
                    var ics = new InternetSharingPrerequisite();
                    if (ics.Fulfilled)
                    {
                        ics.Configure();
                        ics.Resolve();
                    }
                }

                // Update the tunnel service, if everyone is happy
                if (Fulfilled && clientConfigurationsPrerequisite.Fulfilled && new TunnelServicePrerequisite().Fulfilled)
                {
                    // Sync conf to tunnel
                    new WireGuardExe().ExecuteCommand(new SyncConfigurationCommand(WireGuardServerInterfaceName, ServerWGPath));
                }

                Mouse.OverrideCursor = null;
            }

            Refresh();
        }

        public override void Update()
        {
            if (File.Exists(ServerDataPath))
            {
                SaveWG(new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerDataPath)));
            }

            Refresh();
        }

        #endregion

        #region Private methods

        private void SaveData(ServerConfiguration serverConfiguration)
        {
            serverConfiguration.ToConfiguration().SaveToFile(ServerDataPath);
        }

        private void SaveWG(ServerConfiguration serverConfiguration)
        {
            var configuration = serverConfiguration.ToConfiguration<ServerConfiguration>();

            if (Directory.Exists(ClientConfigurationsPrerequisite.ClientDataDirectory))
            {
                foreach (string clientConfigurationFile in Directory.GetFiles(ClientConfigurationsPrerequisite.ClientDataDirectory, "*.conf"))
                {
                    var clientConfiguration = new ClientConfiguration(null).Load<ClientConfiguration>(Configuration.LoadFromFile(clientConfigurationFile));
                    clientConfiguration.ServerPersistentKeepaliveProperty.Value = serverConfiguration.PersistentKeepaliveProperty.Value;
                    
                    configuration = configuration.Merge(clientConfiguration.ToConfiguration<ServerConfiguration>());
                }
            }

            configuration.SaveToFile(ServerWGPath);
        }

        #endregion

        #region Public static properties

        public static string ServerWGPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "server_wg", "wg_server.conf");

        public static string ServerDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "server_data", "wg_server.conf");

        public static string WireGuardServerInterfaceName => Path.GetFileNameWithoutExtension(ServerWGPath);

        #endregion

        #region Public static methods

        public static Network GetNetwork(TimeSpan? timeout = null)
        {
            Network result = default;

            Stopwatch stopwatch = Stopwatch.StartNew();

            do
            {
                // Windows API code pack can show stale adapters, and incorrect names.
                // First, get the real interface here.
                if (NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.Name == WireGuardServerInterfaceName) is { } networkInterface)
                {
                    // Now use the ID to get the network from API code pack
                    if (NetworkListManager.GetNetworks(NetworkConnectivityLevels.All).FirstOrDefault(n => n.Connections.Any(c => c.AdapterId == new Guid(networkInterface.Id))) is { } network)
                    {
                        result = network;
                        break;
                    }
                }
            } while (stopwatch.ElapsedMilliseconds < (timeout?.TotalMilliseconds ?? 0));

            stopwatch.Stop();

            return result;
        }

        #endregion

        #region Private static methods

        private static void SetScopeAddressRegistryValue(string value)
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters", writable: true);
            key?.SetValue("ScopeAddress", value);
        }

        private static string GetScopeAddressRegistryValue()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters", writable: false);
            return key?.GetValue("ScopeAddress")?.ToString();
        }

        #endregion
    }
}
