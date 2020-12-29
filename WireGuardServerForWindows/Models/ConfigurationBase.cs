﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using SharpConfig;
using WireGuardAPI;
using WireGuardAPI.Commands;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public abstract class ConfigurationBase : ObservableObject
    {
        #region Constructor

        protected ConfigurationBase()
        {
            foreach (ConfigurationProperty property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => typeof(ConfigurationProperty).IsAssignableFrom(p.PropertyType))
                .Select(p => p.GetValue(this) as ConfigurationProperty)
                .OrderBy(p => p?.Index))
            {
                Properties.Add(property);
            }
        }

        #endregion

        #region Public (abstract) methods

        public ConfigurationBase Load(Configuration configuration)
        {
            if (configuration.FirstOrDefault(s => s.Name == "Interface") is { } section)
            {
                foreach (Setting setting in section)
                {
                    if (Properties.FirstOrDefault(p => p.PersistentPropertyName == setting.Name) is { } property)
                    {
                        property.Value = setting.StringValue;
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// The serialized representation of this configuration, not targeted to a particular config type, but intended to hold all properties
        /// </summary>
        public Configuration ToConfiguration()
        {
            string sectionName = "Interface";

            var configuration = new Configuration();
            configuration[sectionName].PreComment = NameProperty.Value;
            foreach (ConfigurationProperty property in Properties)
            {
                configuration[sectionName][property.PersistentPropertyName].StringValue = property.Value;
            }

            return configuration;
        }

        /// <summary>
        /// The serialized representation of this configuration, targeted to <see cref="TTarget"/> config file.
        /// </summary>
        public Configuration ToConfiguration<TTarget>() where TTarget : ConfigurationBase
        {
            string sectionName = this is TTarget ? "Interface" : "Peer";

            var configuration = new Configuration();
            configuration[sectionName].PreComment = NameProperty.Value;
            foreach (ConfigurationProperty property in Properties.Where(p => p.TargetTypes.Contains(GetType())))
            {
                configuration[sectionName][property.PersistentPropertyName].StringValue = property.Value;
            }

            return configuration;
        }

        #endregion

        #region Public properties

        public ConfigurationProperty NameProperty => _nameProperty ??= new ConfigurationProperty(this)
        {
            Index = 0,
            PersistentPropertyName = "Name",
            Name = nameof(NameProperty),
            Validation = new EmptyStringValidation(Resources.EmptyClientNameError)
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
                    prop.Value = new WireGuardExe().ExecuteCommand(new GeneratePrivateKeyCommand());
                    Mouse.OverrideCursor = null;
                }
            },
            Validation = new EmptyStringValidation(Resources.KeyValidationError)
        };
        private ConfigurationProperty _privateKeyProperty;

        public ConfigurationProperty PublicKeyProperty => _publicKeyProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "PublicKey",
            Name = nameof(PublicKeyProperty),
            IsReadOnly = true,
            Action = new ConfigurationPropertyAction(dependentProperty: PrivateKeyProperty)
            {
                Name = $"{nameof(PublicKeyProperty)}{nameof(ConfigurationProperty.Action)}",
                Action = (conf, prop) =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    prop.Value = new WireGuardExe().ExecuteCommand(new GeneratePublicKeyCommand(conf.PrivateKeyProperty.Value));
                    Mouse.OverrideCursor = null;
                },
                DependencySatisfiedFunc = prop => string.IsNullOrEmpty(prop.Value) == false
            },
            Validation = new EmptyStringValidation(Resources.KeyValidationError)
        };
        private ConfigurationProperty _publicKeyProperty;

        public ConfigurationProperty PresharedKeyProperty => _presharedKeyProperty ??= new ConfigurationProperty(this)
        {
            PersistentPropertyName = "PresharedKey",
            Name = nameof(PresharedKeyProperty),
            IsReadOnly = true,
            Action = new ConfigurationPropertyAction()
            {
                Name = $"{nameof(PresharedKeyProperty)}{nameof(ConfigurationProperty.Action)}",
                Action = (conf, prop) =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    prop.Value = new WireGuardExe().ExecuteCommand(new GeneratePresharedKeyCommand());
                    Mouse.OverrideCursor = null;
                }
            },
            Validation = new EmptyStringValidation(Resources.KeyValidationError)
        };
        private ConfigurationProperty _presharedKeyProperty;

        public List<ConfigurationProperty> Properties { get; } = new List<ConfigurationProperty>();

        #endregion
    }

    public static class ConfigurationBaseExtensions
    {
        public static TConfig Load<TConfig>(this TConfig @this, Configuration configuration) where TConfig : ConfigurationBase
        {
            return @this.Load(configuration) as TConfig;
        }
    }
}