using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            AddressProperty.DefaultValue = "10.253.0.0/24";
            AddressProperty.Index = 3;

            // Do custom validation on the Address (we want a CIDR notation)
            AddressProperty.Validation = new ConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    string result = default;

                    if (IPNetwork.TryParse(obj.Value, out _) == false)
                    {
                        result = Resources.NetworkAddressValidationError;
                    }
                    else // TryParse succeeded
                    {
                        // IPNetwork.TryParse recognizes single IP addresses as CIDR (with 8 mask).
                        // This is not good, because we want an explicit CIDR for the server.
                        // Therefore, if IPNetwork.TryParse succeeds, and IPAddress.TryParse also succeeds, we have a problem.
                        if (IPAddress.TryParse(obj.Value, out _))
                        {
                            // This is just a regular address. We want CIDR.
                            result = Resources.NetworkAddressValidationError;
                        }
                    }

                    return result;
                }
            };

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
                    string host = string.Join(':', EndpointProperty.Value.Split(':').SkipLast(1));
                    string port = EndpointProperty.Value.Split(':').LastOrDefault();
                    if (string.IsNullOrEmpty(host) == false && string.IsNullOrEmpty(port) == false && port.EndsWith(']') == false)
                    {
                        // It already has IP:Port, so just replace the Port part
                        EndpointProperty.Value = $"{host}:{ListenPortProperty.Value}";
                    }
                    else if (EndpointProperty.Value.StartsWith(':'))
                    {
                        // It has no IP, just :PORT, so replace the port
                        EndpointProperty.Value = $":{ListenPortProperty.Value}";
                    }
                    else if (EndpointProperty.Value.EndsWith(':'))
                    {
                        // It only has IP: and no port, so add the port
                        EndpointProperty.Value = $"{EndpointProperty.Value}{ListenPortProperty.Value}";
                    }
                }
                else
                {
                    // It's an empty string. We can at least populate the port.
                    EndpointProperty.Value = $":{ListenPortProperty.Value}";
                }
            };

            // Resort after changing the index of AddressProperty
            SortProperties();
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
            Name = nameof(AllowedIpsProperty), Description = Resources.ServerAllowedIpsDescription,
            DefaultValue = "0.0.0.0/0",
            Validation = new ConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    string result = default;

                    // Support CSV allowed IPs
                    foreach (string address in obj.Value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()))
                    {
                        if (IPNetwork.TryParse(address, out _) == false)
                        {
                            result = Resources.NetworkAddressValidationError;
                            break;
                        }
                    }

                    return result;
                }
            }
        };
        private ConfigurationProperty _allowedIpsProperty;

        public ConfigurationProperty EndpointProperty => _endpointProperty ??= new ConfigurationProperty(this)
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
                        string host = string.Join(':', obj.Value.Split(':').SkipLast(1));
                        string port = obj.Value.Split(':').LastOrDefault();

                        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port))
                        {
                            result = Resources.EmptyEndpointValidation;
                        }
                        else if (port != ListenPortProperty.Value)
                        {
                            result = Resources.EndpointPortMismatch;
                        }
                        
                        // If we get here, we passed all validation.
                    }

                    return result;
                }
            }
        };
        private ConfigurationProperty _endpointProperty;

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

        #endregion
    }
}
