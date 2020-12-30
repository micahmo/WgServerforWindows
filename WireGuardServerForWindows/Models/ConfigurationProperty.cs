using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace WireGuardServerForWindows.Models
{
    public class ConfigurationProperty : ObservableObject, IDataErrorInfo
    {
        public ConfigurationProperty(ConfigurationBase configuration, ConfigurationProperty dependentProperty = null) =>
            _configuration = configuration;
        
        #region Public properties

        /// <summary>
        /// Allows ordering Properties. Default value is <see cref="int.MaxValue"/>,
        /// so any other value will order the property before properties with no index.
        /// </summary>
        public int Index { get; set; } = int.MaxValue;

        public string Name { get; set; }

        // Maps to the name in the .conf file
        public string PersistentPropertyName { get; set; }

        public string Value
        {
            get => _value ?? DefaultValue;
            set => Set(nameof(Value), ref _value, value);
        }
        private string _value;

        public string DefaultValue { get; set; }

        public bool IsReadOnly { get; set; }

        public bool IsHidden { get; set; }

        public ConfigurationPropertyAction Action { get; set; }

        public ConfigurationPropertyValidation Validation { get; set; }

        public HashSet<Type> TargetTypes { get; } = new HashSet<Type>();

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
