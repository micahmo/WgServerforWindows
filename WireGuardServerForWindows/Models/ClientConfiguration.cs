using System;
using System.IO;
using System.Windows.Input;
using WireGuardAPI;
using WireGuardAPI.Commands;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ClientConfiguration : ConfigurationBase
    {
        #region ConfigurationBase members

        public override ConfigurationBase Load(string configurationFile)
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

        public ConfigurationProperty NameProperty { get; } = new ConfigurationProperty
        {
            PersistentPropertyName = "[Name]", Name = nameof(NameProperty),
            Validation = new EmptyStringValidation(Resources.EmptyClientNameError)
        };

        public ConfigurationProperty PrivateKeyProperty { get; } = new ConfigurationProperty
        {
            PersistentPropertyName = "PrivateKey",
            Name = nameof(PrivateKeyProperty),
            IsReadOnly = true,
            Action = new ConfigurationPropertyAction
            {
                Name = $"{nameof(PrivateKeyProperty)}{nameof(ConfigurationProperty.Action)}",
                Action = obj =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    // TODO
                    //obj.Value = new WireGuardExe().ExecuteCommand(new GeneratePrivateKeyCommand());
                    Mouse.OverrideCursor = null;
                }
            },
            Validation = new EmptyStringValidation(Resources.PrivateKeyValidationError)
        };

        #endregion
    }
}
