using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Input;
using WireGuardAPI;
using WireGuardAPI.Commands;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ServerConfiguration : ConfigurationBase
    {
        #region ConfigurationBase members

        public override ConfigurationBase Load(string configurationFilePath)
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

        public override void Save(string configurationFilePath)
        {
            string contents = string.Join(
                Environment.NewLine,
                ClientConfigurations.Select(c => c.ToString<ServerConfiguration>()).Union(new[] {ToString<ServerConfiguration>()}));

            File.WriteAllText(configurationFilePath, contents);
        }

        public override string ToString<TTarget>()
        {
            string result = default;

            if (typeof(ServerConfiguration).IsAssignableFrom(typeof(TTarget)))
            {
                StringBuilder fileContents = new StringBuilder();
                fileContents.AppendLine("#Server config");
                fileContents.AppendLine("[Interface]");

                foreach (ConfigurationProperty property in Properties)
                {
                    fileContents.AppendLine($"{property.PersistentPropertyName} = {property.Value}");
                }

                result = fileContents.ToString();
            }
            else if (typeof(ClientConfiguration).IsAssignableFrom(typeof(TTarget)))
            {

            }

            return result;
        }

        #endregion

        #region Public properties

        public ConfigurationProperty PrivateKeyProperty { get; } = new ConfigurationProperty
        {
            PersistentPropertyName = "PrivateKey", Name = nameof(PrivateKeyProperty), IsReadOnly = true,
            Action = new ConfigurationPropertyAction
            {
                Name = $"{nameof(PrivateKeyProperty)}{nameof(ConfigurationProperty.Action)}",
                Action = obj =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    obj.Value = new WireGuardExe().ExecuteCommand(new GeneratePrivateKeyCommand());
                    Mouse.OverrideCursor = null;
                }
            },
            Validation = new EmptyStringValidation(Resources.PrivateKeyValidationError)
        };

        public ConfigurationProperty ListenPortProperty { get; } = new ConfigurationProperty
        {
            PersistentPropertyName = "ListenPort", Name = nameof(ListenPortProperty), DefaultValue = "51820",
            Validation = new ConfigurationPropertyValidation
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

        public ConfigurationProperty AddressProperty { get; } = new ConfigurationProperty
        {
            PersistentPropertyName = "Address", Name = nameof(AddressProperty), DefaultValue = "10.253.0.2/32",
            Validation = new ConfigurationPropertyValidation
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

        // The list of client peers accepted by this server
        public List<ClientConfiguration> ClientConfigurations { get; } = new List<ClientConfiguration>();

        #endregion
    }
}
