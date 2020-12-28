using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ClientConfiguration : ConfigurationBase
    {
        #region Constructor

        public ClientConfiguration(ClientConfigurationList parentList) =>
            _parentList = parentList;

        #endregion

        #region ConfigurationBase members

        public override ConfigurationBase Load(string configurationFilePath)
        {
            throw new NotImplementedException();
        }

        public override void Save(string configurationFile)
        {
            string contents = string.Join(
                Environment.NewLine,
                ServerConfiguration.ToString<ClientConfiguration>(),
                ToString<ClientConfiguration>());

            File.WriteAllText(configurationFile, contents);
        }

        public override string ToString<TTarget>()
        {
            string result = default;

            if (typeof(ServerConfiguration).IsAssignableFrom(typeof(TTarget)))
            {

            }
            else if (typeof(ClientConfiguration).IsAssignableFrom(typeof(TTarget)))
            {

            }

            return result;
        }

        #endregion

        #region Public properties

        // Every client has a reference to its server
        public ServerConfiguration ServerConfiguration { get; set; }

        public ConfigurationProperty NameProperty => _nameProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "[Name]", Name = nameof(NameProperty),
            Validation = new EmptyStringValidation(Resources.EmptyClientNameError),
            Action = new ConfigurationPropertyAction
            {
                Name = Resources.Delete,
                Action = (conf, prop) =>
                {
                    MessageBoxResult res = MessageBox.Show(Resources.ConfirmDeleteClient, Resources.Confirm, MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.Yes)
                    {
                        (conf as ClientConfiguration)?.RemoveClientCommand?.Execute(null);
                    }
                }
            }
        };
        private ConfigurationProperty _nameProperty;

        public ConfigurationProperty PrivateKeyProperty => _privateKeyProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "PrivateKey",
            Name = nameof(PrivateKeyProperty),
            IsReadOnly = true,
            Action = new ConfigurationPropertyAction
            {
                Name = $"{nameof(PrivateKeyProperty)}{nameof(ConfigurationProperty.Action)}",
                Action = (conf, prop) =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    // TODO
                    //obj.Value = new WireGuardExe().ExecuteCommand(new GeneratePrivateKeyCommand());
                    Mouse.OverrideCursor = null;
                }
            },
            Validation = new EmptyStringValidation(Resources.PrivateKeyValidationError)
        };
        private ConfigurationProperty _privateKeyProperty;

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
