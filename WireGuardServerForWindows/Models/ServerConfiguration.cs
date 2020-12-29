using System.Collections.Generic;
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
            AllowedIpsProperty.TargetTypes.Add(typeof(ClientConfiguration));

            // Set some properties that are unique to server
            AddressProperty.DefaultValue = "10.253.0.0/24";
            AddressProperty.Index = 3;

            // Resort after changing the index of AddressProperty
            Properties.Sort((a, b) => a.Index - b.Index);
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

        public ConfigurationProperty AllowedIpsProperty => _allowedIpsProperty ??= new ConfigurationProperty(this)
        {
            Index = 2,
            PersistentPropertyName = "AllowedIPs",
            Name = nameof(AllowedIpsProperty),
            DefaultValue = "0.0.0.0/0",
            Validation = new ConfigurationPropertyValidation
            {
                // Reuse AddressProperty validation
                Validate = obj => AddressProperty.Validation?.Validate?.Invoke(obj)
            }
        };
        private ConfigurationProperty _allowedIpsProperty;

        // The list of client peers accepted by this server
        public List<ClientConfiguration> ClientConfigurations { get; } = new List<ClientConfiguration>();

        #endregion
    }
}
