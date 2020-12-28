using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GalaSoft.MvvmLight;

namespace WireGuardServerForWindows.Models
{
    public abstract class ConfigurationBase : ObservableObject
    {
        #region Constructor

        protected ConfigurationBase()
        {
            foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => typeof(ConfigurationProperty).IsAssignableFrom(p.PropertyType)))
            {
                Properties.Add(property.GetValue(this) as ConfigurationProperty);
            }
        }

        #endregion

        #region Public (abstract) methods

        public abstract ConfigurationBase Load(string configurationFile);

        public abstract void Save(string configurationFile);

        /// <summary>
        /// The string representation of this configuration, targeted to <see cref="TTarget"/> config file.
        /// </summary>
        public abstract string ToString<TTarget>() where TTarget : ConfigurationBase;

        #endregion

        #region Public properties

        public string Name { get; set; }

        public List<ConfigurationProperty> Properties { get; } = new List<ConfigurationProperty>();

        #endregion
    }

    public static class ConfigurationBaseExtensions
    {
        public static TConfig Load<TConfig>(this TConfig configuration, string configurationFilePath) where TConfig : ConfigurationBase
        {
            return configuration.Load(configurationFilePath) as TConfig;
        }
    }
}
