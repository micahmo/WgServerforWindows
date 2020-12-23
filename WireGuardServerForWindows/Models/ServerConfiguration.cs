using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using WireGuardAPI;
using WireGuardAPI.Commands;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ServerConfiguration : ObservableObject
    {
        public ServerConfiguration()
        {
            foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => typeof(ServerConfigurationProperty).IsAssignableFrom(p.PropertyType)))
            {
                Properties.Add(property.GetValue(this) as ServerConfigurationProperty);
            }
        }

        public ServerConfiguration Load(string configurationFilePath)
        {
            foreach (string line in File.ReadAllLines(configurationFilePath))
            {
                List<string> parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries).Select(str => str.Trim()).ToList();
                string propertyName = parts.FirstOrDefault();
                string value = parts.LastOrDefault();

                if (propertyName is { } && value is { } &&
                    Properties.FirstOrDefault(p => p.PersistentPropertyName == propertyName) is { } property)
                {
                    property.Value = value;
                }
            }

            return this;
        }

        public void Save(string configurationFilePath)
        {
            StringBuilder fileContents = new StringBuilder();
            fileContents.AppendLine("#Server config");
            fileContents.AppendLine("[Interface]");

            foreach (ServerConfigurationProperty property in Properties)
            {
                fileContents.AppendLine($"{property.PersistentPropertyName} = {property.Value}");
            }

            File.WriteAllText(configurationFilePath, fileContents.ToString());
        }

        #region Public properties

        public List<ServerConfigurationProperty> Properties { get; } = new List<ServerConfigurationProperty>();

        public ServerConfigurationProperty PrivateKeyProperty { get; } = new ServerConfigurationProperty
        {
            PersistentPropertyName = "PrivateKey", Name = nameof(PrivateKeyProperty),
            Action = new ServerConfigurationPropertyAction
            {
                Name = $"{nameof(PrivateKeyProperty)}{nameof(ServerConfigurationProperty.Action)}",
                Action = obj =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    obj.Value = new WireGuardExe().ExecuteCommand(new GeneratePrivateKeyCommand());
                    Mouse.OverrideCursor = null;
                }
            },
            Validation = new ServerConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    string result = default;

                    if (string.IsNullOrEmpty(obj.Value))
                    {
                        result = Resources.PrivateKeyValidationError;
                    }

                    return result;
                }
            }
        };

        public ServerConfigurationProperty ListenPortProperty { get; } = new ServerConfigurationProperty
        {
            PersistentPropertyName = "ListenPort", Name = nameof(ListenPortProperty), DefaultValue = "51820",
            Validation = new ServerConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    string result = default;

                    if (int.TryParse(obj.Value, out int port))
                    {
                        if (port < 0 || port > 65535)
                        {
                            result = Resources.PortRangeValidationError;
                        }
                    }
                    else
                    {
                        result = Resources.PortValidationError;
                    }

                    return result;
                }
            }
        };

        public ServerConfigurationProperty AddressProperty { get; } = new ServerConfigurationProperty
        {
            PersistentPropertyName = "Address", Name = nameof(AddressProperty), DefaultValue = "10.253.0.2/32",
            Validation = new ServerConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    string result = default;

                    if (IPNetwork.TryParse(obj.Value, out _) == false)
                    {
                        result = Resources.NetworkAddressValidationError;
                    }

                    return result;
                }
            }
        };

        #endregion
    }
}
