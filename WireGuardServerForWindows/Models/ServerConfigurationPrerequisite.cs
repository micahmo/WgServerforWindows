using System;
using System.IO;
using System.Windows.Input;
using WireGuardServerForWindows.Controls;
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

                if (File.Exists(ConfigurationPath) == false)
                {
                    result = false;
                    ErrorMessage = Resources.ServerConfigurationMissingErrorMessage;
                }
                else
                {
                    // The file exists, make sure it has all the fields
                    ServerConfiguration serverConfiguration = new ServerConfiguration().Load(ConfigurationPath);

                    foreach (ServerConfigurationProperty property in serverConfiguration.Properties)
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
            if (File.Exists(ConfigurationPath) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigurationPath));
                using (File.Create(ConfigurationPath));
            }

            Configure();
        }

        public override void Configure()
        {
            ServerConfiguration serverConfiguration = new ServerConfiguration().Load(ConfigurationPath);
            ConfigurationEditor configurationEditor = new ConfigurationEditor {DataContext = serverConfiguration};

            Mouse.OverrideCursor = Cursors.Wait;
            if (configurationEditor.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                serverConfiguration.Save(ConfigurationPath);
                Mouse.OverrideCursor = null;
            }

            Refresh();
        }

        #region Public properties

        public string ConfigurationPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "wg_server.conf");

        #endregion
    }
}
