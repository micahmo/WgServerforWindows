using System;
using System.IO;
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

        public override bool Fulfilled => File.Exists(_configurationPath);

        public override void Resolve()
        {
            if (File.Exists(_configurationPath) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_configurationPath));
                File.Create(_configurationPath);
            }

            Configure();
        }

        public override void Configure()
        {
            ServerConfiguration serverConfiguration = new ServerConfiguration(new WireGuardExe()).Load(_configurationPath);
            ConfigurationEditor configurationEditor = new ConfigurationEditor { DataContext = serverConfiguration };
            if (configurationEditor.ShowDialog() == true)
            {
                serverConfiguration.Save(_configurationPath);
            }
        }

        #region Private fields

        private string _configurationPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W", "wg_server.conf");

        #endregion
    }
}
