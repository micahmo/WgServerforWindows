﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Input;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
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
            ClientPresharedKeyProperty.TargetTypes.Add(typeof(ClientConfiguration));
            PublicKeyProperty.TargetTypes.Add(typeof(ClientConfiguration));
            ClientAllowedRoutableIpsProperty.TargetTypes.Add(typeof(ClientConfiguration));
            EndpointProperty.TargetTypes.Add(typeof(ClientConfiguration));

            // Set some properties that are unique to server
            AddressProperty.DefaultValue = "10.253.0.0/24";
            AddressProperty.Index = 3;

            // Do custom validation on the Address (we want a CIDR notation)
            AddressProperty.Validation = new ConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    var allAddressesAreValid = obj.Value.Split(',').Select(input => input.Trim()).All(input =>
                    {
                        // It must be a valid parsable IP (v4 or v6)...
                        if (!IPNetwork.TryParse(input, out var address))
                        {
                            return false;
                        }
                        // ...in the internal (local) addresses range
                        if (!address.AddressFamily.Equals(AddressFamily.InterNetwork) && !address.AddressFamily.Equals(AddressFamily.InterNetworkV6))
                        {
                            return false;
                        }
                        // ...with at least one subnet mask IP address available in range
                        if (address.Usable < 1)
                        {
                            return false;
                        }

                        return true;
                    });

                    return allAddressesAreValid ? default : Resources.NetworkAddressValidationError;
                }
            };

            EndpointProperty.Action = new ConfigurationPropertyAction(this)
            {
                Name = $"{nameof(EndpointProperty)}{nameof(ConfigurationProperty.Action)}",
                Description = Resources.EndpointPropertyActionDescription,
                Action = async (conf, prop) =>
                {
                    // Immediately disable the action so the user can't invoke it again.
                    EndpointProperty.Action.DependencySatisfiedFunc = _ => false;

                    string ip = null;

                    WaitCursor.SetOverrideCursor(Cursors.Wait);

                    try
                    {
                        var httpClient = new HttpClient();
                        ip = await httpClient.GetStringAsync("https://api.ipify.org");
                    }
                    catch
                    {
                        // Swallow
                    }

                    if (string.IsNullOrEmpty(ip))
                    {
                        // Failed. Indicate such to the user for a period of time.
                        EndpointProperty.Action.Name = nameof(Resources.FailedToIdentify);
                    }
                    else
                    {
                        // We got it! Update the value and tell the user.
                        EndpointProperty.Host = ip;
                        EndpointProperty.Action.Name = nameof(Resources.Updated);
                    }

                    WaitCursor.SetOverrideCursor(null);

                    // Wait a short time so the user can see the status message
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    // Reset the state of the action
                    EndpointProperty.Action.Name = $"{nameof(EndpointProperty)}{nameof(ConfigurationProperty.Action)}";
                    EndpointProperty.Action.Description = Resources.EndpointPropertyActionDescription;

                    // Lastly, re-enable the action.
                    EndpointProperty.Action.DependencySatisfiedFunc = null;
                }
            };

            ListenPortProperty.PropertyChanged += (_, args) =>
            {
                EndpointProperty.Port = ListenPortProperty.Value;
            };

            // Private key validation is unique to the server.
            // (On the client, the property can be manually erased and saved without error.)
            PrivateKeyProperty.Validation = new EmptyStringValidation(Resources.KeyValidationError);

            // Resort after changing the index of AddressProperty
            SortProperties();
        }

        #endregion

        #region Public properties

        public ConfigurationProperty ListenPortProperty => _listenPortProperty ??= new ConfigurationProperty(this)
        {
            Index = 1,
            PersistentPropertyName = "ListenPort",
            Name = nameof(ListenPortProperty),
            DefaultValue = "51820",
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
            Description = Resources.ServerAllowedIpsDescription,
            DefaultValue = "0.0.0.0/0",
            Validation = new ConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    if (string.IsNullOrEmpty(obj.Value))
                    {
                        return Resources.NetworkAddressValidationError;
                    }

                    // Support CSV allowed IPs
                    foreach (string address in obj.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()))
                    {
                        if (IPNetwork.TryParse(address, out _) == false)
                        {
                            return Resources.NetworkAddressValidationError;
                        }
                    }

                    return default;
                }
            }
        };
        private ConfigurationProperty _allowedIpsProperty;

        public EndpointConfigurationProperty EndpointProperty => _endpointProperty ??= new EndpointConfigurationProperty(this)
        {
            Index = 3,
            PersistentPropertyName = "Endpoint",
            Name = nameof(EndpointProperty),
            DefaultValue = $":{ListenPortProperty.Value}",
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
                        if (string.IsNullOrEmpty(EndpointProperty.Host) || string.IsNullOrEmpty(EndpointProperty.Port))
                        {
                            result = Resources.EmptyEndpointValidation;
                        }
                        else if (EndpointProperty.Port != ListenPortProperty.Value)
                        {
                            result = Resources.EndpointPortMismatch;
                        }

                        // If we get here, we passed all validation.
                    }

                    return result;
                }
            }
        };
        private EndpointConfigurationProperty _endpointProperty;

        // Note: Although this property is configured on the server, it goes in the peer (client) section of the server's config,
        // which means it also has to be defined on the client, targeted to the server's config.
        // The client should return the server's value, and the server should not target this property to any config type.
        public ConfigurationProperty PersistentKeepaliveProperty => _persistentKeepaliveProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "PersistentKeepalive", // Don't really need this since it isn't saved from here
            Name = nameof(PersistentKeepaliveProperty),
            DefaultValue = 0.ToString(),
            Validation = new ConfigurationPropertyValidation
            {
                Validate = prop =>
                {
                    string result = default;

                    if (string.IsNullOrEmpty(prop.Value) || int.TryParse(prop.Value, out _) == false)
                    {
                        result = Resources.PersistentKeepaliveValidationError;
                    }

                    return result;
                }
            }
        };
        private ConfigurationProperty _persistentKeepaliveProperty;

        // Note: This is really a client property, but it goes in the peer (server) section of the client's config.
        // So we'll trick the config generator by putting it in the server, targeting it to the client, and returning the client's value.
        // This property needs a Client Context to evaluate.
        public ConfigurationProperty ClientPresharedKeyProperty => _clientPresharedKeyProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "PresharedKey",
            IsHidden = true,
            IsCalculated = true,
            GetValueFunc = () => _clientContext?.PresharedKeyProperty.Value
        };
        private ConfigurationProperty _clientPresharedKeyProperty;

        // Note: This is really a client property, but it goes in the peer (server) section of the client's config.
        // So we'll trick the config generator by putting it in the server, targeting it to the client, and returning the client's value.
        // This property needs a Client Context to evaluate.
        public ConfigurationProperty ClientAllowedRoutableIpsProperty => _clientAllowedRoutableIpsProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "AllowedIPs",
            IsHidden = true,
            IsCalculated = true,
            GetValueFunc = () => _clientContext?.AllowedRoutableIpsProperty.Value
        };
        private ConfigurationProperty _clientAllowedRoutableIpsProperty;

        /// <summary>
        /// This is a calculated field that generates a Server IP address based on the current <see cref="ServerConfiguration.AddressProperty"/> property.
        /// Returns an empty string if the IP address cannot be generated for any reason.
        /// </summary>
        public string IpAddress
        {
            get
            {
                string result = string.Empty;

                try
                {
                    IPNetwork network = IPNetwork.Parse(AddressProperty.Value);
                    result = network.ListIPAddress().Skip(1).FirstOrDefault()?.ToString() ?? string.Empty;
                }
                catch
                {
                    // Should never come here, because we should only invoke this method if the AddressProperty has already passed validation.
                    // But just to be safe...
                }

                return result;
            }
        }

        /// <summary>
        /// This is a calculated field that generates the subnet from the current <see cref="ServerConfiguration.AddressProperty"/> property.
        /// Returns an empty string if the IP address cannot be generated for any reason.
        /// </summary>
        public string Subnet
        {
            get
            {
                string result = string.Empty;

                try
                {
                    IPNetwork network = IPNetwork.Parse(AddressProperty.Value);
                    result = network.Cidr.ToString();
                }
                catch
                {
                    // Should never come here, because we should only invoke this method if the AddressProperty has already passed validation.
                    // But just to be safe...
                }

                return result;
            }
        }

        #endregion

        #region Public methods

        public ServerConfiguration WithClientContext(ClientConfiguration clientConfiguration)
        {
            _clientContext = clientConfiguration;
            return this;
        }

        #endregion

        #region Private fields

        /// <summary>
        /// This field should be set during a <see cref="ConfigurationBase.ToConfiguration"/> call
        /// when certain properties (e.g., <see cref="ClientPresharedKeyProperty"/>) need client info in order to evaluate.
        /// </summary>
        /// <remarks>
        /// Should be set fluently via <see cref="WithClientContext(ClientConfiguration)"/>.
        /// </remarks>
        private ClientConfiguration _clientContext;

        #endregion
    }

    /// <summary>
    /// An extension of <see cref="ConfigurationProperty"/> that is specific to <see cref="ServerConfiguration.EndpointProperty"/>,
    /// containing additional methods for parsing and setting parts of the endpoint.
    /// </summary>
    public class EndpointConfigurationProperty : ConfigurationProperty
    {
        public EndpointConfigurationProperty(ConfigurationBase configuration, ConfigurationProperty dependentProperty = null)
            : base(configuration, dependentProperty)
        {
        }

        /// <summary>
        /// Provides access to the host portion of the value. Will be empty string if not present. Can be set without affecting the port.
        /// </summary>
        public string Host
        {
            get => string.Join(':', (Value ?? string.Empty).Split(':').SkipLast(1));
            set
            {
                if (string.IsNullOrEmpty(Value) == false)
                {
                    if (string.IsNullOrEmpty(Host) == false && string.IsNullOrEmpty(Port) == false && Port.EndsWith(']') == false)
                    {
                        // It already has IP:Port, so just replace the IP part
                        Value = $"{value}:{Port}";
                    }
                    else if (Value.StartsWith(':'))
                    {
                        // It has no IP, just :PORT, so add the IP
                        Value = $"{value}{Value}";
                    }
                    else if (Value.EndsWith(':'))
                    {
                        // It only has IP: and no port, so replace the IP
                        Value = $"{value}:";
                    }
                }
                else
                {
                    // It's an empty string. We can at least populate the IP.
                    Value = $"{value}:";
                }
            }
        }

        /// <summary>
        /// Provides access to the port portion of the value. Will be empty string if not present. Can be set without affecting the host.
        /// </summary>
        public string Port
        {
            get => (Value ?? string.Empty).Split(':').LastOrDefault();
            set
            {
                if (string.IsNullOrEmpty(Value) == false)
                {
                    if (string.IsNullOrEmpty(Host) == false && string.IsNullOrEmpty(Port) == false && Port.EndsWith(']') == false)
                    {
                        // It already has IP:Port, so just replace the Port part
                        Value = $"{Host}:{value}";
                    }
                    else if (Value.StartsWith(':'))
                    {
                        // It has no IP, just :PORT, so replace the port
                        Value = $":{value}";
                    }
                    else if (Value.EndsWith(':'))
                    {
                        // It only has IP: and no port, so add the port
                        Value = $"{Value}{value}";
                    }
                }
                else
                {
                    // It's an empty string. We can at least populate the port.
                    Value = $":{value}";
                }
            }
        }
    }
}
