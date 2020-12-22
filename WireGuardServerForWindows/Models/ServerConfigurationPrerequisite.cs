using System;
using System.IO;
using System.Windows.Input;
using WireGuardAPI;
using WireGuardServerForWindows.Controls;

namespace WireGuardServerForWindows.Models
{
    public class ServerConfigurationPrerequisite : PrerequisiteItem
    {
        public ServerConfigurationPrerequisite() : base
        (
            title: "Server Configuration",
            successMessage: "Server configuration file (.conf) found.",
            errorMessage: "Server configuration file (.conf) not found.",
            resolveText: "Create and edit server configuration",
            configureText: "Edit server configuration"
        ) { }

        public override bool Fulfilled
        {
            get
            {
                bool result = true;

                if (File.Exists(_configurationPath) == false)
                {
                    result = false;
                    ErrorMessage = "Server configuration file (.conf) not found.";
                }
                else
                {
                    // The file exists, make sure it has all the fields
                    ServerConfiguration serverConfiguration = new ServerConfiguration(new WireGuardExe()).Load(_configurationPath);
                    if (string.IsNullOrEmpty(serverConfiguration.Validate()) == false)
                    {
                        result = false;
                        ErrorMessage = "Server configuration not completed. Some fields are missing or incorrect.";
                    }
                }

                return result;
            }
        }

        public override void Resolve()
        {
            if (File.Exists(_configurationPath) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_configurationPath));
                using (File.Create(_configurationPath));
            }

            Configure();
        }

        public override void Configure()
        {
            ServerConfiguration serverConfiguration = new ServerConfiguration(new WireGuardExe()).Load(_configurationPath);
            ConfigurationEditor configurationEditor = new ConfigurationEditor { DataContext = serverConfiguration };
            if (configurationEditor.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                serverConfiguration.Save(_configurationPath);
                Mouse.OverrideCursor = null;
            }

            Refresh();
        }

        #region Private fields

        private string _configurationPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "wg_server.conf");

        #endregion
    }
}
