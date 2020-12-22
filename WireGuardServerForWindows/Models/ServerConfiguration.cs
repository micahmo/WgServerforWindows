using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using WireGuardAPI;
using WireGuardAPI.Commands;

namespace WireGuardServerForWindows.Models
{
    public class ServerConfiguration : ObservableObject
    {
        public ServerConfiguration(WireGuardExe wireGuardExe)
            => (WireGuardExe, Commands) = (wireGuardExe, new ServerConfigurationCommands(this));

        public ServerConfiguration Load(string configurationFilePath)
        {
            foreach (string line in File.ReadAllLines(configurationFilePath))
            {
                List<string> parts = line.Split('=', StringSplitOptions.RemoveEmptyEntries).Select(str => str.Trim()).ToList();
                string propertyName = parts.FirstOrDefault();
                string value = parts.LastOrDefault();

                if (propertyName is { } && value is { } &&
                    GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public) is PropertyInfo propertyInfo &&
                    Attribute.IsDefined(propertyInfo, typeof(ServerConfigPropertyAttribute)))
                {
                    propertyInfo.SetValue(this, value);
                }
            }

            return this;
        }

        public void Save(string configurationFilePath)
        {
            StringBuilder fileContents = new StringBuilder();
            fileContents.AppendLine("#Server config");
            fileContents.AppendLine("[Interface]");

            foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                fileContents.AppendLine($"{property.Name} = {property.GetValue(this)}");
            }

            File.WriteAllText(configurationFilePath, fileContents.ToString());
        }

        #region Public properties

        public ServerConfigurationCommands Commands { get; }

        [ServerConfigProperty]
        public string PrivateKey
        {
            get => _privateKey;
            set => Set(nameof(PrivateKey), ref _privateKey, value);
        }
        private string _privateKey;

        [ServerConfigProperty]
        public string ListenPort
        {
            get => _listenPort;
            set => Set(nameof(ListenPort), ref _listenPort, value);
        }
        private string _listenPort = "51820";

        [ServerConfigProperty]
        public string Address
        {
            get => _address;
            set => Set(nameof(Address), ref _address, value);
        }
        private string _address;

        #endregion

        #region Internal properties

        internal WireGuardExe WireGuardExe { get; }

        #endregion
    }

    public class ServerConfigurationCommands
    {
        public ServerConfigurationCommands(ServerConfiguration serverConfiguration) =>
            ServerConfiguration = serverConfiguration;

        private ServerConfiguration ServerConfiguration { get; }

        #region ICommands

        public ICommand GeneratePrivateKeyCommand => _generatePrivateKeyCommand ??= new RelayCommand(GeneratePrivateKey);
        private RelayCommand _generatePrivateKeyCommand;

        #endregion

        #region Command implementations

        private void GeneratePrivateKey()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            ServerConfiguration.PrivateKey = ServerConfiguration.WireGuardExe.ExecuteCommand(new GeneratePrivateKeyCommand());

            Mouse.OverrideCursor = null;
        }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class ServerConfigPropertyAttribute : Attribute { }
}
