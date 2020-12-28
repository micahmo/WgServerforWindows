using System;
using System.ComponentModel;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace WireGuardServerForWindows.Models
{
    public class ConfigurationProperty : ObservableObject, IDataErrorInfo
    {
        public ConfigurationProperty(ConfigurationBase configuration) =>
            _configuration = configuration;
        
        #region Public properties

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

        public ConfigurationPropertyAction Action { get; set; }

        public ConfigurationPropertyValidation Validation { get; set; }

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
    }
}
