using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace WgServerforWindows.Models
{
    public class ConfigurationProperty : ObservableObject, IDataErrorInfo
    {
        public ConfigurationProperty(ConfigurationBase configuration, ConfigurationProperty dependentProperty = null) =>
            _configuration = configuration;
        
        #region Public properties

        /// <summary>
        /// Allows ordering Properties. Default value is: <code>int.MaxValue / 2</code>
        /// Any value less than that will order properties before those with no custom value.
        /// Any value higher than that will order properties after those with no custom value.
        /// </summary>
        public int Index { get; set; } = int.MaxValue / 2;

        public string Name { get; set; }

        // Maps to the name in the .conf file
        public string PersistentPropertyName { get; set; }

        public string Description { get; set; }

        public string Value
        {
            get => _value ?? GetValueFunc?.Invoke();
            set => Set(nameof(Value), ref _value, value);
        }
        private string _value;

        /// <summary>
        /// Allows defining a method for evaluating the value, rather than setting it
        /// </summary>
        public Func<string> GetValueFunc { get; set; }

        public string DefaultValue
        {
            // Without explicitly using init-only setter, only apply the default value if the value is empty.
            // This allows for real empty values to be saved later.
            set
            {
                if (string.IsNullOrEmpty(Value))
                {
                    Value = value;
                }
            }
        }

        public bool IsReadOnly { get; set; }

        public bool IsEnabled { get; set; } = true;

        public bool IsHidden { get; set; }

        /// <summary>
        /// Specifies whether the property's value is calculated on the fly based on in-memory data.
        /// </summary>
        /// <remarks>
        /// Properties for whom <see cref="IsCalculated"/> is true will NOT be read from or persisted to data confs (but may be persisted to wg confs).
        /// </remarks>
        public bool IsCalculated { get; set; } = false;

        public ConfigurationPropertyAction Action { get; set; }

        public ConfigurationPropertyValidation Validation { get; set; }

        public HashSet<Type> TargetTypes { get; } = new HashSet<Type>();

        /// <summary>
        /// An action to be invoked after the configuration has been loaded
        /// </summary>
        public Action<ConfigurationBase> OnLoadAction { get; set; }

        #region Commands

        public ICommand ExecuteActionCommand => _executeActionCommand ??= new RelayCommand(() =>
        {
            Action.Action?.Invoke(_configuration, this);
        });
        private RelayCommand _executeActionCommand;

        #endregion

        #endregion

        #region Private fields

        private readonly ConfigurationBase _configuration;

        #endregion

        #region IDataErrorInfo members

        public string Error => throw new NotImplementedException();

        public string this[string columnName]
        {
            get
            {
                string result = default;

                if (columnName == nameof(Value))
                {
                    result = Validation?.Validate?.Invoke(this);
                }

                return result;
            }
        }

        #endregion

        #region Overrides

        // Override ToString for debugging
        public override string ToString() => $"{PersistentPropertyName}: '{Value}'";

        #endregion
    }
}
