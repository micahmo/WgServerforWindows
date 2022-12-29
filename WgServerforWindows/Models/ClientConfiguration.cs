using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using QRCoder;
using SharpConfig;
using WgAPI;
using WgAPI.Commands;
using WgServerforWindows.Extensions;
using WgServerforWindows.Properties;
using ECCLevel = QRCoder.QRCodeGenerator.ECCLevel;
using Image = System.Windows.Controls.Image;

namespace WgServerforWindows.Models
{
    public class ClientConfiguration : ConfigurationBase
    {
        #region Constructor

        public ClientConfiguration(ClientConfigurationList parentList)
        {
            _parentList = parentList;

            // Client properties
            PrivateKeyProperty.TargetTypes.Add(GetType());
            FullDnsProperty.TargetTypes.Add(GetType());
            AddressProperty.TargetTypes.Add(GetType());

            // Server properties
            PresharedKeyProperty.TargetTypes.Add(typeof(ServerConfiguration));
            PublicKeyProperty.TargetTypes.Add(typeof(ServerConfiguration));
            ServerPersistentKeepaliveProperty.TargetTypes.Add(typeof(ServerConfiguration));

            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath));
            string serverAddresses = serverConfiguration.AddressProperty.Value;
            IPNetwork serverIpv4Network = null;
            IPNetwork serverIpv6Network = null;
            foreach (var serverIPNetwork in ExtractIPs(serverConfiguration.AddressProperty.Value).Select(IPNetwork.Parse))
            {
                switch (serverIPNetwork.AddressFamily)
                {
                    case AddressFamily.InterNetwork: serverIpv4Network = serverIPNetwork; break;
                    case AddressFamily.InterNetworkV6: serverIpv6Network = serverIPNetwork; break;
                    default: break;
                }
            }
            string allowedIpsDefault = serverConfiguration.AllowedIpsProperty.Value;

            // Add support for generating client IP
            AddressProperty.Action = new ConfigurationPropertyAction(this)
            {
                Name = nameof(Resources.GenerateFromServerAction),
                Description = string.Format(Resources.GenerateClientAddressActionDescription, serverAddresses),
                DependentProperty = serverConfiguration.AddressProperty,
                DependencySatisfiedFunc = prop => string.IsNullOrEmpty(prop.Validation?.Validate?.Invoke(prop)),
                Action = (conf, prop) =>
                {
                    IPAddress inputIPv4Address = null;
                    IPAddress inputIPv6Address = null;
                    foreach (var inputIPNetwork in ExtractIPs(prop.Value).Select(IPNetwork.Parse))
                    {
                        switch (inputIPNetwork.AddressFamily)
                        {
                            case AddressFamily.InterNetwork: inputIPv4Address = inputIPNetwork.Network; break;
                            case AddressFamily.InterNetworkV6: inputIPv6Address = inputIPNetwork.Network; break;
                            default: break;
                        }
                    }

                    var clientAddresses = new List<string>(2);
                    if (serverIpv4Network != null)
                    {
                        var ip = inputIPv4Address ?? serverIpv4Network.ListIPAddress(FilterEnum.Usable).Skip(1).FirstOrDefault(address => !OtherClientAddresses.Contains(address));
                        clientAddresses.Add($"{ip}/32");
                    }
                    if (serverIpv6Network != null)
                    {
                        var ip = inputIPv6Address ?? serverIpv6Network.ListIPAddress(FilterEnum.Usable).FirstOrDefault(address => !OtherClientAddresses.Contains(address));
                        clientAddresses.Add($"{ip}/128");
                    }

                    WaitCursor.SetOverrideCursor(Cursors.Wait);
                    prop.Value = string.Join(", ", clientAddresses);
                    WaitCursor.SetOverrideCursor(null);
                }
            };

            // Do custom validation on the Address (we expect a CIDR with /32 for IPv4, or /128 for IPv6)
            AddressProperty.Validation = new ConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    if (string.IsNullOrEmpty(obj.Value))
                    {
                        return Resources.ClientAddressValidationError;
                    }

                    var allAddressesAreValid = ExtractIPs(obj.Value).All(input =>
                    {
                        if (!IPNetwork.TryParse(input, out var ipNetwork))
                        {
                            return false;
                        }

                        switch (ipNetwork.AddressFamily)
                        {
                            case AddressFamily.InterNetwork:
                                {
                                    // must have /32 CIDR suffix, must be in the server network subnet usable IPs range, and not occupied by other clients.
                                    return ipNetwork.Cidr.Equals(32) &&
                                        (serverIpv4Network?.Contains(ipNetwork) ?? false) &&
                                        serverIpv4Network.FirstUsable.AsIPNetwork().CompareTo(ipNetwork) == -1 &&
                                        serverIpv4Network.LastUsable.AsIPNetwork().CompareTo(ipNetwork) == 1 &&
                                        !OtherClientAddresses.Contains(ipNetwork.Network);
                                }
                            case AddressFamily.InterNetworkV6:
                                {
                                    // must have /128 CIDR suffix, must be in the server network subnet usable IPs range, and not occupied by other clients.
                                    return ipNetwork.Cidr.Equals(128) &&
                                        (serverIpv6Network?.Contains(ipNetwork) ?? false) &&
                                        serverIpv6Network.FirstUsable.AsIPNetwork().CompareTo(ipNetwork) == -1 &&
                                        !OtherClientAddresses.Contains(ipNetwork.Network);
                                }
                            default:
                                {
                                    return false;
                                }
                        }
                    });

                    return allAddressesAreValid ? default : Resources.ClientAddressValidationError;
                }
            };

            AllowedRoutableIpsProperty.Action = new ConfigurationPropertyAction(this)
            {
                Name = nameof(Resources.PopulateFromServerAction),
                Description = string.Format(Resources.PopulateClientAllowedIpsActionDescription, allowedIpsDefault),
                DependentProperty = serverConfiguration.AllowedIpsProperty,
                DependencySatisfiedFunc = prop => string.IsNullOrEmpty(prop.Validation?.Validate?.Invoke(prop)),
                Action = (conf, prop) =>
                {
                    prop.Value = allowedIpsDefault;
                }
            };

            // Initial value is server value
            AllowedRoutableIpsProperty.Value = allowedIpsDefault;

            // The client generates the PSK
            PresharedKeyProperty.Action = new ConfigurationPropertyAction(this)
            {
                Name = $"{nameof(PresharedKeyProperty)}{nameof(ConfigurationProperty.Action)}",
                Action = (conf, prop) =>
                {
                    WaitCursor.SetOverrideCursor(Cursors.Wait);
                    prop.Value = new WireGuardExe().ExecuteCommand(new GeneratePresharedKeyCommand());
                    WaitCursor.SetOverrideCursor(null);
                }
            };

            // Allowed IPs is special, because for the server, it's the same as the Address property for the client
            var allowedIpsProperty = new ConfigurationProperty(this)
            {
                PersistentPropertyName = "AllowedIPs",
                Value = AddressProperty.Value,
                IsHidden = true
            };
            allowedIpsProperty.TargetTypes.Add(typeof(ServerConfiguration));
            AddressProperty.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(AddressProperty.Value))
                {
                    allowedIpsProperty.Value = AddressProperty.Value;
                }
            };
            Properties.Add(allowedIpsProperty);

            // Adjust index of properties and resort
            AddressProperty.Index = 1;
            DnsProperty.Index = 2;
            SortProperties();

            Properties.ForEach(p => p.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(p.Value))
                {
                    GenerateQrCodeAction.RaisePropertyChanged(nameof(GenerateQrCodeAction.DependencySatisfied));
                    ExportConfigurationFileAction.RaisePropertyChanged(nameof(ExportConfigurationFileAction.DependencySatisfied));
                }
            });
        }

        #endregion

        #region Public properties

        public string Name => string.IsNullOrEmpty(NameProperty.Value) ? _guidName ??= Guid.NewGuid().ToString() : NameProperty.Value;
        private string _guidName;

        // Do not target this to any WG config. We only want it in the data.
        public ConfigurationProperty IndexProperty => _index ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "Index",
            DefaultValue = 0.ToString(),
            IsHidden = true
        };
        private ConfigurationProperty _index;

        /// <summary>
        /// This is a funny one. This is first defined on the server. Then the user can import that default value into the client config.
        /// Then the property needs to go to the server and be targeted to the client's config (under the server/peer section).
        /// </summary>
        public ConfigurationProperty AllowedRoutableIpsProperty => _allowedIpsProperty ??= new ConfigurationProperty(this)
        {
            Index = 2,
            // This doesn't match any WG Conf property, but it doesn't matter since the actual WG value will come from the server's client-targeted property.
            PersistentPropertyName = "AllowedRoutableIPs",
            Name = nameof(AllowedRoutableIpsProperty),
            Description = Resources.ServerAllowedIpsDescription,
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

        public ConfigurationProperty DnsProperty => _dnsProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "DNS",
            Name = nameof(DnsProperty),
            DefaultValue = "8.8.8.8, 1.1.1.1",
            Validation = new ConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    string result = default;

                    // Only validate if not empty. (No DNS is valid.)
                    if (string.IsNullOrEmpty(obj.Value) == false)
                    {
                        foreach (string address in obj.Value.Split(new[] { ',' }).Select(a => a.Trim()))
                        {
                            // We don't want any CIDR here, so parse with IPAddress instead of IPNetwork
                            if (IPAddress.TryParse(address, out _) == false)
                            {
                                result = Resources.DnsAddressValidationError;
                                break;
                            }
                        }
                    }

                    return result;
                }
            }
        };
        private ConfigurationProperty _dnsProperty;

        public ConfigurationProperty DnsSearchDomainsProperty => _dnsSearchDomainsProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "DNSSearchDomains",
            Name = nameof(DnsSearchDomainsProperty),
            Description = Resources.DnsSearchDomainsPropertyDescription,
            Validation = new ConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    if (!string.IsNullOrEmpty(obj.Value))
                    {
                        // If they've specified a value, make sure it's a list of non-empty, comma-separated strings.
                        IEnumerable<string> dnsSearchDomains = obj.Value.Split(new[] { ',' }).Select(a => a.Trim()).ToList();
                        if (dnsSearchDomains.Any(string.IsNullOrWhiteSpace))
                        {
                            return Resources.DnsSearchDomainsValidationError;
                        }

                        // If any of the domains contains a space, it is invalid.
                        if (dnsSearchDomains.Any(a => a.Any(char.IsWhiteSpace)))
                        {
                            return Resources.DnsSearchDomainsValidationError;
                        }
                    }

                    // If we get here, everything's good.
                    return default;
                }
            }
        };
        private ConfigurationProperty _dnsSearchDomainsProperty;

        /// <summary>
        /// Combines the values of <see cref="DnsProperty"/> and <see cref="DnsSearchDomainsProperty"/> for the final configuration file.
        /// </summary>
        public ConfigurationProperty FullDnsProperty => _fullDnsProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "DNS",
            IsHidden = true,
            IsCalculated = true,
            GetValueFunc = () => string.Join(',', (
                DnsProperty.Value?.Split(new[] { ',' }) ?? Enumerable.Empty<string>()).Select(a => a.Trim()).Where(a => !string.IsNullOrWhiteSpace(a)).Concat((
                DnsSearchDomainsProperty.Value?.Split(new[] { ',' }) ?? Enumerable.Empty<string>()).Select(a => a.Trim()).Where(a => !string.IsNullOrWhiteSpace(a))))
        };
        private ConfigurationProperty _fullDnsProperty;

        // Note: This is really a server property, but it goes in the in the (peer) client section of the server's config.
        // So we'll trick the config generator by putting it in the client, targeting it to the server, and returning the server's value,
        // which the server will set on the client while saving
        public ConfigurationProperty ServerPersistentKeepaliveProperty => _persistentKeepaliveProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "PersistentKeepalive",
            IsHidden = true
        };
        private ConfigurationProperty _persistentKeepaliveProperty;

        // Note: This is a client-specific property. It goes in the peer (client) section of the server's config, and is thus targeted to the server config type.
        // However, it also goes in the peer (server) section of the client config.
        // Therefore, it must also be defined on the server, targeted to the client, and return this client's value.
        public ConfigurationProperty PresharedKeyProperty => _presharedKeyProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "PresharedKey",
            Name = nameof(PresharedKeyProperty),
            IsReadOnly = true, // Don't allow manual clearing; this would require a full resync anyway. See README.
            Index = int.MaxValue // Put it at the end
        };
        private ConfigurationProperty _presharedKeyProperty;

        public ConfigurationPropertyAction DeleteAction => _deleteAction ??= new ConfigurationPropertyAction(this)
        {
            Name = nameof(Resources.DeleteAction),
            Action = (conf, prop) =>
            {
                MessageBoxResult res = MessageBox.Show(Resources.ConfirmDeleteClient, Resources.Confirm, MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    (conf as ClientConfiguration)?.RemoveClientCommand?.Execute(null);
                }
            }
        };
        private ConfigurationPropertyAction _deleteAction;

        public ConfigurationPropertyAction GenerateQrCodeAction => _generateQrCodeAction ??= new ConfigurationPropertyAction(this)
        {
            Name = nameof(Resources.GenerateQrCodeAction),
            Action = (conf, prop) =>
            {
                Configuration configuration = ToConfiguration<ClientConfiguration>();

                Configuration serverConfiguration = default;
                if (File.Exists(ServerConfigurationPrerequisite.ServerDataPath))
                {
                    serverConfiguration = new ServerConfiguration()
                        .Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath))
                        .WithClientContext(conf as ClientConfiguration)
                        .ToConfiguration<ClientConfiguration>();
                }

                using MemoryStream memoryStream = new MemoryStream();
                configuration.Merge(serverConfiguration).SaveToStream(memoryStream);

                using StreamReader streamReader = new StreamReader(memoryStream);
                // Have to seek to the beginning before reading
                memoryStream.Seek(0, SeekOrigin.Begin);

                QRCode code = new QRCode(new QRCodeGenerator().CreateQrCode(streamReader.ReadToEnd(), ECCLevel.Q));
                Bitmap image = code.GetGraphic(20);

                new Window
                {
                    Content = new Image
                    {
                        Source = image.ToImageSource()
                    },
                    Width = 300,
                    Height = 300,
                    Title = string.Format(Resources.ClientConfigurationTitle, conf.NameProperty.Value)
                }.ShowDialog();
            },
            DependencySatisfiedFunc = _ => Properties.All(p => string.IsNullOrEmpty(p.Validation?.Validate?.Invoke(p))),
        };
        private ConfigurationPropertyAction _generateQrCodeAction;

        public ConfigurationPropertyAction ExportConfigurationFileAction => _exportConfigurationFileAction ??= new ConfigurationPropertyAction(this)
        {
            Name = nameof(Resources.ExportConfigurationFileAction),
            Action = (conf, prop) =>
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog { FileName = $"{conf.NameProperty.Value}.conf", Filter = "Configuration Files (*.conf)|*.conf" };
                if (saveFileDialog.ShowDialog() == true)
                {
                    Configuration configuration = ToConfiguration<ClientConfiguration>();

                    Configuration serverConfiguration = default;
                    if (File.Exists(ServerConfigurationPrerequisite.ServerDataPath))
                    {
                        serverConfiguration = new ServerConfiguration()
                            .Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath))
                            .WithClientContext(conf as ClientConfiguration)
                            .ToConfiguration<ClientConfiguration>();
                    }

                    configuration.Merge(serverConfiguration).SaveToFile(saveFileDialog.FileName);
                }
            },
            DependencySatisfiedFunc = _ => Properties.All(p => string.IsNullOrEmpty(p.Validation?.Validate?.Invoke(p))),
        };
        private ConfigurationPropertyAction _exportConfigurationFileAction;

        public ICommand RemoveClientCommand => _removeClientCommand ??= new RelayCommand(() =>
        {
            using (new WaitCursor(dispatcherPriority: DispatcherPriority.Render, restoreCursorToNull: true))
            {
                _parentList?.List.Remove(this);
            }
        });
        private RelayCommand _removeClientCommand;

        #endregion

        #region Private fields

        private readonly ClientConfigurationList _parentList;

        private IEnumerable<IPAddress> OtherClientAddresses
        {
            get => _parentList.List.Where(item => item.Name != this.Name).SelectMany(c => c.AddressProperty.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => IPNetwork.Parse(a.Trim()).Network));
        }

        private IEnumerable<string> ExtractIPs(string value)
        {
            return (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).Where(x => !string.IsNullOrEmpty(x));
        }

        #endregion
    }
}
