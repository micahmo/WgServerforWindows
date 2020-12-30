using System;
using System.Collections.Generic;
using System.Windows.Input;
using WireGuardAPI;
using WireGuardAPI.Commands;
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
            //AddressProperty.TargetTypes.Add(GetType());
            ListenPortProperty.TargetTypes.Add(GetType());

            // Client properties
            PresharedKeyProperty.TargetTypes.Add(typeof(ClientConfiguration));
            PublicKeyProperty.TargetTypes.Add(typeof(ClientConfiguration));
            AllowedIpsProperty.TargetTypes.Add(typeof(ClientConfiguration));
            EndpointProperty.TargetTypes.Add(typeof(ClientConfiguration));

            // Set some properties that are unique to server
            AddressProperty.DefaultValue = "10.253.0.1/24";
            AddressProperty.Index = 3;

            // Resort after changing the index of AddressProperty
            SortProperties();

            // The Server actually generates the pre-shared key
            PresharedKeyProperty.Action = new ConfigurationPropertyAction(this)
            {
                Name = $"{nameof(PresharedKeyProperty)}{nameof(ConfigurationProperty.Action)}",
                Action = (conf, prop) =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    prop.Value = new WireGuardExe().ExecuteCommand(new GeneratePresharedKeyCommand());
                    Mouse.OverrideCursor = null;
                }
            };

            ListenPortProperty.PropertyChanged += (_, __) =>
            {
                if (string.IsNullOrEmpty(EndpointProperty.Value) == false)
                {
                    var parts = EndpointProperty.Value.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        EndpointProperty.Value = $"{parts[0]}:{ListenPortProperty.Value}";
                    }
                    else if (EndpointProperty.Value.StartsWith(':'))
                    {
                        EndpointProperty.Value = $":{ListenPortProperty.Value}";
                    }
                }
            };
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

        public ConfigurationProperty EndpointProperty => _endpointProperty ??= new ConfigurationProperty(this)
        {
            Index = 3,
            PersistentPropertyName = "Endpoint",
            Name = nameof(EndpointProperty),
            DefaultValue = $":{ListenPortProperty.DefaultValue}",
            Validation = new ConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    string result = default;

                    if (string.IsNullOrEmpty(obj.Value))
                    {
                        result = Resources.EmptyEndpointValidation;
                    }
                    else
                    {
                        var endpointParts = obj.Value.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
                        if (endpointParts.Length == 2)
                        {
                            if (endpointParts[1] != ListenPortProperty.Value)
                            {
                                result = Resources.EndpointPortMismatch;
                            }
                        }
                        else
                        {
                            result = Resources.EmptyEndpointValidation;
                        }
                    }

                    return result;
                }
            }
        };
        private ConfigurationProperty _endpointProperty;

        // The list of client peers accepted by this server
        public List<ClientConfiguration> ClientConfigurations { get; } = new List<ClientConfiguration>();

        #endregion
    }
}
