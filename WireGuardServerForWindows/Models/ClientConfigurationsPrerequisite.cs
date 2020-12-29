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
        public ClientConfigurationsPrerequisite() : base
        (
            title: Resources.ClientConfigurations,
            successMessage: Resources.ClientConfigurationsSuccessMessage,
            errorMessage: Resources.ClientConfigurationsMissingErrorMessage,
            resolveText: Resources.ClientConfigurationsResolveText,
            configureText: Resources.ClientConfigurationsResolveText
        ) { }

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

                // Save to Data
                foreach (ClientConfiguration clientConfiguration in clientConfigurations.List)
                {
                    var configuration = clientConfiguration.ToConfiguration();
                    configuration.SaveToFile(Path.Combine(ClientDataDirectory, $"{clientConfiguration.NameProperty.Value}.conf"));
                }

                // Save to WG
                foreach (ClientConfiguration clientConfiguration in clientConfigurations.List)
                {
                    Configuration configuration = clientConfiguration.ToConfiguration<ClientConfiguration>();

                    Configuration serverConfiguration = default;
                    if (File.Exists(ServerConfigurationPrerequisite.ServerDataPath))
                    {
                        serverConfiguration = new ServerConfiguration()
                            .Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath))
                            .ToConfiguration<ClientConfiguration>();
                    }

                    configuration.Merge(serverConfiguration).SaveToFile(Path.Combine(ClientWGDirectory, $"{clientConfiguration.NameProperty.Value}.conf"));
                }

                Mouse.OverrideCursor = null;
            }

            Refresh();
        }

        #region Public static properties

        public static string ClientDataDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "clients_data");

        #endregion

        #region Private static properties

        private static string ClientWGDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "clients_wg");

        #endregion
    }
}
