using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using SharpConfig;
using WireGuardServerForWindows.Properties;

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

            // Add support for deleting client config (not supported in server)
            NameProperty.Action = new ConfigurationPropertyAction
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

            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath));
            string serverIp = serverConfiguration.AddressProperty.Value;

            // Add support for generating client IP
            AddressProperty.Action = new ConfigurationPropertyAction
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

                    Mouse.OverrideCursor = Cursors.Wait;

                    var existingAddresses = parentList.List.Select(c => c.AddressProperty.Value);

                    // Find the first address that isn't used by another client
                    prop.Value = possibleAddresses.FirstOrDefault(a => existingAddresses.Contains(a.ToString()) == false)?.ToString();

                    Mouse.OverrideCursor = null;
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
        }

        #endregion

        #region Public properties

        public string Name => string.IsNullOrEmpty(NameProperty.Value) ? _guidName ??= Guid.NewGuid().ToString() : NameProperty.Value;
        private string _guidName;

        public ConfigurationProperty DnsProperty => _dnsProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "DNS",
            Name = nameof(DnsProperty),
            DefaultValue = "1.1.1.1, 8.8.8.8",
            Validation = new ConfigurationPropertyValidation
            {
                Validate = obj =>
                {
                    string result = default;

                    if (string.IsNullOrEmpty(obj.Value))
                    {
                        result = Resources.NetworkAddressValidationError;
                    }
                    else
                    {
                        foreach (string address in obj.Value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (IPNetwork.TryParse(address, out _) == false)
                            {
                                result = Resources.NetworkAddressValidationError;
                                break;
                            }
                        }
                    }

                    return result;
                }
            }
        };
        private ConfigurationProperty _dnsProperty;

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
