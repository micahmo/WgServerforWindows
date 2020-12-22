using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using WireGuardAPI;
using WireGuardAPI.Commands;

namespace WireGuardServerForWindows.Models
{
    public class ServerConfiguration : ObservableObject, IDataErrorInfo
    {
        public ServerConfiguration(WireGuardExe wireGuardExe)
            => (WireGuardExe, Commands) = (wireGuardExe, new ServerConfigurationCommands(this));

        public ServerConfiguration Load(string configurationFilePath)
        {
            foreach (string line in File.ReadAllLines(configurationFilePath))
            {
                List<string> parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries).Select(str => str.Trim()).ToList();
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

            foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(prop => Attribute.IsDefined(prop, typeof(ServerConfigPropertyAttribute))))
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
        private string _address = "192.168.1.1/24";

        #endregion

        #region Internal properties

        internal WireGuardExe WireGuardExe { get; }

        #endregion

        #region IDataErrorInfo members

        // Not used by WPF binding validation
        public string Error => throw new NotImplementedException();

        public string this[string columnName]
        {
            get
            {
                string result = default;
                
                switch (columnName)
                {
                    case nameof(PrivateKey):
                        if (string.IsNullOrEmpty(PrivateKey))
                        {
                            result = "Private key must not be empty";
                        }
                        break;
                    case nameof(ListenPort):
                        if (int.TryParse(ListenPort, out int port))
                        {
                            if (port < 0 || port > 65535)
                            {
                                result = "Port must be between 0 and 65535.";
                            }
                        }
                        else
                        {
                            result = "Port must be numerical.";
                        }
                        break;
                    case nameof(Address):
                        if (IPNetwork.TryParse(Address, out _) == false)
                        {
                            result = "Network must be in valid CIDR notation. For example: 192.168.1.1/24";
                        }
                        break;
                    default:
                        break;
                }

                return result;
            }
        }

        /// <summary>
        /// Validates all fields. Returns null if all fields pass. Returns the first error if not.
        /// </summary>
        public string Validate()
        {
            return this[nameof(PrivateKey)] ?? this[nameof(ListenPort)] ?? this[nameof(Address)];
        }

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
