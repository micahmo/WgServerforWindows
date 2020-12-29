using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using SharpConfig;
using WireGuardServerForWindows.Controls;
using WireGuardServerForWindows.Extensions;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ClientConfigurationsPrerequisite : PrerequisiteItem
    {
        #region Constructor

        public ClientConfigurationsPrerequisite() : base
        (
            title: Resources.ClientConfigurations,
            successMessage: Resources.ClientConfigurationsSuccessMessage,
            errorMessage: Resources.ClientConfigurationsMissingErrorMessage,
            resolveText: Resources.ClientConfigurationsResolveText,
            configureText: Resources.ClientConfigurationsResolveText
        ) { }

        #endregion

        #region PrerequisiteItem members

        public override bool Fulfilled
        {
            get
            {
                bool result = true;

                if (Directory.Exists(ClientWGDirectory) == false || Directory.GetFiles(ClientWGDirectory).Any() == false)
                {
                    result = false;
                    ErrorMessage = Resources.ClientConfigurationsMissingErrorMessage;
                }
                else
                {
                    // Validate all of the client(s)
                    foreach (string clientConfigurationFile in Directory.GetFiles(ClientDataDirectory, "*.conf"))
                    {
                        var clientConfiguration = new ClientConfiguration(null).Load<ClientConfiguration>(Configuration.LoadFromFile(clientConfigurationFile));

                        foreach (ConfigurationProperty property in clientConfiguration.Properties)
                        {
                            if (string.IsNullOrEmpty(property.Validation?.Validate?.Invoke(property)) == false)
                            {
                                result = false;
                                ErrorMessage = Resources.ClientConfigurationsIncompleteErrorMessage;
                                goto finish;
                            }
                        }
                    }
                }

                finish:
                return result;
            }
        }

        public override void Resolve()
        {
            if (Directory.Exists(ClientDataDirectory) == false)
            {
                Directory.CreateDirectory(ClientDataDirectory);
            }

            if (Directory.Exists(ClientWGDirectory) == false)
            {
                Directory.CreateDirectory(ClientWGDirectory);
            }

            Configure();
        }

        public override void Configure()
        {
            ClientConfigurationList clientConfigurations = new ClientConfigurationList();

            foreach (string clientConfigurationFile in Directory.GetFiles(ClientDataDirectory, "*.conf"))
            {
                clientConfigurations.List.Add(new ClientConfiguration(clientConfigurations).Load<ClientConfiguration>(Configuration.LoadFromFile(clientConfigurationFile)));
            }

            ClientConfigurationEditorWindow clientConfigurationEditorWindow = new ClientConfigurationEditorWindow {DataContext = clientConfigurations};

            Mouse.OverrideCursor = Cursors.Wait;
            if (clientConfigurationEditorWindow.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Delete the existing files (can't rely on updating them since the name of the client may have changed)
                foreach (string clientConfigurationFile in Directory.GetFiles(ClientDataDirectory, "*.conf"))
                {
                    File.Delete(clientConfigurationFile);
                }

                foreach (string clientConfigurationFile in Directory.GetFiles(ClientWGDirectory, "*.conf"))
                {
                    File.Delete(clientConfigurationFile);
                }

                // Save to Data
                foreach (ClientConfiguration clientConfiguration in clientConfigurations.List)
                {
                    SaveData(clientConfiguration);
                }

                // Save to WG
                foreach (ClientConfiguration clientConfiguration in clientConfigurations.List)
                {
                    SaveWG(clientConfiguration);
                }

                // Update server
                new ServerConfigurationPrerequisite().Update();

                Mouse.OverrideCursor = null;
            }

            Refresh();
        }

        public override void Update()
        {
            if (Directory.Exists(ClientDataDirectory))
            {
                foreach (string clientConfigurationFile in Directory.GetFiles(ClientDataDirectory, "*.conf"))
                {
                    SaveWG(new ClientConfiguration(null).Load<ClientConfiguration>(Configuration.LoadFromFile(clientConfigurationFile)));
                }
            }

            Refresh();
        }

        #endregion

        #region Private methods

        private void SaveData(ClientConfiguration clientConfiguration)
        {
            var configuration = clientConfiguration.ToConfiguration();
            configuration.SaveToFile(Path.Combine(ClientDataDirectory, $"{clientConfiguration.Name}.conf"));
        }

        private void SaveWG(ClientConfiguration clientConfiguration)
        {
            Configuration configuration = clientConfiguration.ToConfiguration<ClientConfiguration>();

            Configuration serverConfiguration = default;
            if (File.Exists(ServerConfigurationPrerequisite.ServerDataPath))
            {
                serverConfiguration = new ServerConfiguration()
                    .Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath))
                    .ToConfiguration<ClientConfiguration>();
            }

            configuration.Merge(serverConfiguration).SaveToFile(Path.Combine(ClientWGDirectory, $"{clientConfiguration.Name}.conf"));
        }

        #endregion

        #region Public static properties

        public static string ClientDataDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "clients_data");

        #endregion

        #region Private static properties

        private static string ClientWGDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "clients_wg");

        #endregion
    }
}
