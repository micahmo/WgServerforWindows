using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ServerConfiguration : ConfigurationBase
    {
        #region Constructor

        public ServerConfiguration()
        {
            // Server properties
            PrivateKeyProperty.TargetTypes.Add(GetType());
            AddressProperty.TargetTypes.Add(GetType());
            ListenPortProperty.TargetTypes.Add(GetType());

            // Client properties
            PresharedKeyProperty.TargetTypes.Add(typeof(ClientConfiguration));
            PublicKeyProperty.TargetTypes.Add(typeof(ClientConfiguration));
        }

        #endregion

        #region ConfigurationBase members

        public override ConfigurationBase Load(string configurationFilePath)
        {
            bool start = false;
            bool stop = false;

            foreach (string line in File.ReadAllLines(configurationFilePath))
            {
                if (line == "[Interface]")
                {
                    start = true;
                    continue;
                }
                else if (line == "[Peer]")
                {
                    stop = true;
                    continue;
                }

                if (start && !stop)
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
            }

            return this;
        }

        public override void Save(string configurationFilePath)
        {
            string contents = string.Join(
                Environment.NewLine,
                new[] { ToString<ServerConfiguration>() }.Union(ClientConfigurations.Select(c => c.ToString<ServerConfiguration>())));

            File.WriteAllText(configurationFilePath, contents);
        }

        #endregion

        #region Public properties

        public ConfigurationProperty ListenPortProperty => _listenPortProperty ??= new ConfigurationProperty(this)
        {
            Index = 1,
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
        private ConfigurationProperty _listenPortProperty;

        public ConfigurationProperty AddressProperty => _addressProperty ??= new ConfigurationProperty(this)
        {
            Index = 2,
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
        private ConfigurationProperty _addressProperty;

        // The list of client peers accepted by this server
        public List<ClientConfiguration> ClientConfigurations { get; } = new List<ClientConfiguration>();

        #endregion
    }
}
