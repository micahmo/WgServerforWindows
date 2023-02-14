using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Net;
using SharpConfig;
using WgAPI;
using WgAPI.Commands;
using WgServerforWindows.Controls;
using WgServerforWindows.Extensions;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class ServerConfigurationPrerequisite : PrerequisiteItem
    {
        #region Constructor

        public ServerConfigurationPrerequisite() : this(new OpenServerConfigDirectorySubCommand(), new ChangeServerConfigDirectorySubCommand())
        {
        }

        public ServerConfigurationPrerequisite(
            OpenServerConfigDirectorySubCommand openServerConfigDirectorySubCommand, 
            ChangeServerConfigDirectorySubCommand changeServerConfigDirectorySubCommand) : base
        (
            title: Resources.ServerConfiguration,
            successMessage: Resources.ServerConfigurationSuccessMessage,
            errorMessage: Resources.ServerConfigurationMissingErrorMessage,
            resolveText: Resources.ServerConfigurationConfigureText,
            configureText: Resources.ServerConfigurationConfigureText
        )
        {
            SubCommands.Add(openServerConfigDirectorySubCommand);
            SubCommands.Add(changeServerConfigDirectorySubCommand);
        }

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
            EnsureConfigFile();

            Configure();
        }

        public override void Configure()
        {
            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerDataPath));
            string originalServerIp = serverConfiguration.AddressProperty.Value;
            
            ServerConfigurationEditorWindow serverConfigurationEditor = new ServerConfigurationEditorWindow {DataContext = serverConfiguration};

            WaitCursor.SetOverrideCursor(Cursors.Wait);
            if (serverConfigurationEditor.ShowDialog() == true)
            {
                WaitCursor.SetOverrideCursor(Cursors.Wait);

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
                if (Fulfilled && (clientConfigurationsPrerequisite.Fulfilled || !ClientConfigurationsPrerequisite.AnyClients) && new TunnelServicePrerequisite().Fulfilled)
                {
                    // Sync conf to tunnel
                    string output = new WireGuardExe().ExecuteCommand(new SyncConfigurationCommand(WireGuardServerInterfaceName, ServerWGPath), out int exitCode);

                    if (exitCode != 0)
                    {
                        // Notify the user that there was an error syncing the server conf.
                        WaitCursor.SetOverrideCursor(null);

                        new UnhandledErrorWindow
                        {
                            DataContext = new UnhandledErrorWindowModel
                            {
                                Title = Resources.Error,
                                Text = $"{Resources.ServerSyncError}{Environment.NewLine}{Environment.NewLine}{output}",
                                Exception = new Exception(output)
                            }
                        }.ShowDialog();
                    }
                }

                WaitCursor.SetOverrideCursor(null);
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

                    if (clientConfiguration.IsEnabledProperty.Value == true.ToString())
                    {
                        clientConfiguration.ServerPersistentKeepaliveProperty.Value = serverConfiguration.PersistentKeepaliveProperty.Value;
                        configuration = configuration.Merge(clientConfiguration.ToConfiguration<ServerConfiguration>());
                    }
                }
            }

            configuration.SaveToFile(ServerWGPath);
        }

        #endregion

        #region Public static properties

        public static string ServerConfigDirectory =>
            !string.IsNullOrWhiteSpace(AppSettings.Instance.CustomServerConfigDirectory) && Directory.Exists(AppSettings.Instance.CustomServerConfigDirectory)
                ? AppSettings.Instance.CustomServerConfigDirectory
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W");

        public static string ServerWGDirectory => Path.Combine(ServerConfigDirectory, "server_wg");

        public static string ServerWGPath => Path.Combine(ServerWGDirectory, "wg_server.conf");

        public static string ServerDataDirectory => Path.Combine(ServerConfigDirectory, "server_data");

        public static string ServerDataPath => Path.Combine(ServerDataDirectory, "wg_server.conf");

        public static string WireGuardServerInterfaceName => Path.GetFileNameWithoutExtension(ServerWGPath);

        #endregion

        #region Public static methods

        public static void EnsureConfigFile()
        {
            if (Directory.Exists(Path.GetDirectoryName(ServerDataPath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ServerDataPath));
            }

            if (File.Exists(ServerDataPath) == false)
            {
                File.Create(ServerDataPath).Dispose();
            }

            if (Directory.Exists(Path.GetDirectoryName(ServerWGPath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ServerWGPath));
            }

            if (File.Exists(ServerWGPath) == false)
            {
                File.Create(ServerWGPath).Dispose();
            }
        }

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
