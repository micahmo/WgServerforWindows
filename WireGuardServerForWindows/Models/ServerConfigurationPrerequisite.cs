using System;
using System.IO;
using System.Windows.Input;
using SharpConfig;
using WireGuardServerForWindows.Controls;
using WireGuardServerForWindows.Extensions;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ServerConfigurationPrerequisite : PrerequisiteItem
    {
        public ServerConfigurationPrerequisite() : base
        (
            title: Resources.ServerConfiguration,
            successMessage: Resources.ServerConfigurationSuccessMessage,
            errorMessage: Resources.ServerConfigurationMissingErrorMessage,
            resolveText: Resources.ServerConfigurationResolveText,
            configureText: Resources.ServerConfigurationConfigureText
        ) { }

        public override bool Fulfilled
        {
            get
            {
                bool result = true;

                if (File.Exists(ServerWGPath) == false)
                {
                    result = false;
                    ErrorMessage = Resources.ServerConfigurationMissingErrorMessage;
                }
                else
                {
                    // The file exists, make sure it has all the fields
                    var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerDataPath));

                    foreach (ConfigurationProperty property in serverConfiguration.Properties)
                    {
                        if (string.IsNullOrEmpty(property.Validation?.Validate?.Invoke(property)) == false)
                        {
                            result = false;
                            ErrorMessage = Resources.ServerConfigurationIncompleteErrorMessage;
                            break;
                        }
                    }
                }

                return result;
            }
        }

        public override void Resolve()
        {
            if (Directory.Exists(Path.GetDirectoryName(ServerDataPath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ServerDataPath));
            }

            if (File.Exists(ServerDataPath) == false)
            {
                using (File.Create(ServerDataPath));
            }

            if (Directory.Exists(Path.GetDirectoryName(ServerWGPath)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ServerWGPath));
            }

            if (File.Exists(ServerWGPath) == false)
            {
                using (File.Create(ServerWGPath));
            }

            Configure();
        }

        public override void Configure()
        {
            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerDataPath));
            ServerConfigurationEditorWindow serverConfigurationEditor = new ServerConfigurationEditorWindow {DataContext = serverConfiguration};

            Mouse.OverrideCursor = Cursors.Wait;
            if (serverConfigurationEditor.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Save to Data
                serverConfiguration.ToConfiguration().SaveToFile(ServerDataPath);

                // Save to WG
                var configuration = serverConfiguration.ToConfiguration<ServerConfiguration>();

                if (Directory.Exists(ClientConfigurationsPrerequisite.ClientDataDirectory))
                {
                    foreach (string clientConfigurationFile in Directory.GetFiles(ClientConfigurationsPrerequisite.ClientDataDirectory, "*.conf"))
                    {
                        configuration = configuration.Merge(new ClientConfiguration(null)
                            .Load<ClientConfiguration>(Configuration.LoadFromFile(clientConfigurationFile))
                            .ToConfiguration<ServerConfiguration>());
                    }
                }

                configuration.SaveToFile(ServerWGPath);

                Mouse.OverrideCursor = null;
            }

            Refresh();
        }

        #region Public static properties

        public static string ServerWGPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "server_wg", "wg_server.conf");

        public static string ServerDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "server_data", "wg_server.conf");

        #endregion
    }
}
