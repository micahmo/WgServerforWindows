using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using QRCoder;
using SharpConfig;
using WireGuardServerForWindows.Extensions;
using WireGuardServerForWindows.Properties;
using ECCLevel = QRCoder.QRCodeGenerator.ECCLevel;
using Image = System.Windows.Controls.Image;

namespace WireGuardServerForWindows.Models
{
    public class ClientConfiguration : ConfigurationBase
    {
        #region Constructor

        public ClientConfiguration(ClientConfigurationList parentList)
        {
            _parentList = parentList;

            // Client properties
            PrivateKeyProperty.TargetTypes.Add(GetType());
            DnsProperty.TargetTypes.Add(GetType());
            AddressProperty.TargetTypes.Add(GetType());

            // Server properties
            PresharedKeyProperty.TargetTypes.Add(typeof(ServerConfiguration));
            PublicKeyProperty.TargetTypes.Add(typeof(ServerConfiguration));
            ServerPersistentKeepaliveProperty.TargetTypes.Add(typeof(ServerConfiguration));

            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath));
            string serverIp = serverConfiguration.AddressProperty.Value;

            // Add support for generating client IP
            AddressProperty.Action = new ConfigurationPropertyAction(this)
            {
                Name = nameof(Resources.GenerateFromServerAction),
                Description = string.Format(Resources.GenerateClientAddressActionDescription, serverIp),
                DependentProperty = serverConfiguration.AddressProperty,
                DependencySatisfiedFunc = prop => string.IsNullOrEmpty(prop.Validation?.Validate?.Invoke(prop)),
                Action = (conf, prop) =>
                {
                    IPNetwork serverNetwork = IPNetwork.Parse(serverConfiguration.AddressProperty.Value);
                    var possibleAddresses = serverNetwork.ListIPAddress().Skip(2).SkipLast(1).ToList(); // Skip reserved .0 and .1 and .255.

                    // If the current address is already in range, we're done
                    if (possibleAddresses.Select(a => a.ToString()).Contains(prop.Value))
                    {
                        return;
                    }

                    WaitCursor.SetOverrideCursor(Cursors.Wait);

                    var existingAddresses = parentList.List.Select(c => c.AddressProperty.Value);

                    // Find the first address that isn't used by another client
                    prop.Value = possibleAddresses.FirstOrDefault(a => existingAddresses.Contains(a.ToString()) == false)?.ToString();

                    WaitCursor.SetOverrideCursor(null);
                }
            };

            // Do custom validation on the Address (we want a specific IP or a CIDR with /32)
            AddressProperty.Validation = new ConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    string result = default;

                    // First, try parsing with IPNetwork to see if it's in CIDR format
                    if (IPNetwork.TryParse(obj.Value, out var network))
                    {
                        // At this point, we know it's a valid network. Let's see how many addresses are in range
                        if (network.Usable > 1)
                        {
                            // It's CIDR, but it defines more than one address.
                            // However, IPNetwork has a quirk that parses single addresses (without mask) as a range.
                            // So now let's see if it's a single address
                            if (IPAddress.TryParse(obj.Value, out _) == false)
                            {
                                // If we get here, it passed CIDR parsing, but it defined more than one address (i.e., had a mask). It's bad!
                                result = Resources.ClientAddressValidationError;
                            }
                            // Else, it's a single address as parsed by IPAddress, so we're good!
                        }
                        // Else
                        // It's in CIDR notation and only defines a single address (/32) so we're good!
                    }
                    else
                    {
                        // Not even IPNetwork could parse it, so it's really bad!
                        result = Resources.ClientAddressValidationError;
                    }

                    return result;
                }
            };

            // The client only copies the PSK from the server
            PresharedKeyProperty.Action = new ConfigurationPropertyAction(this)
            {
                Name = $"Client{nameof(PresharedKeyProperty)}{nameof(ConfigurationProperty.Action)}",
                Action = (conf, prop) =>
                {
                    WaitCursor.SetOverrideCursor(Cursors.Wait);
                    prop.Value = serverConfiguration.PresharedKeyProperty.Value;
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
                        foreach (string address in obj.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()))
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

        // Note: This is really a server property, but it goes in the in the (peer) client section of the server's config.
        // So we'll trick the config generator by putting it in the client, targeting it to the server, and returning the server's value,
        // which the server will set on the client while saving
        public ConfigurationProperty ServerPersistentKeepaliveProperty => _persistentKeepaliveProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "PersistentKeepalive",
            IsHidden = true
        };
        private ConfigurationProperty _persistentKeepaliveProperty;

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
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog {FileName = $"{conf.NameProperty.Value}.conf", Filter = "Configuration Files (*.conf)|*.conf"};
                if (saveFileDialog.ShowDialog() == true)
                {
                    Configuration configuration = ToConfiguration<ClientConfiguration>();

                    Configuration serverConfiguration = default;
                    if (File.Exists(ServerConfigurationPrerequisite.ServerDataPath))
                    {
                        serverConfiguration = new ServerConfiguration()
                            .Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath))
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

        #endregion
    }
}
