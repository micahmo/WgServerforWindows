using System;
using System.ComponentModel;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace WireGuardServerForWindows.Models
{
    public class ServerConfigurationProperty : ObservableObject, IDataErrorInfo
    {
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

        public ServerConfigurationPropertyAction Action { get; set; }

        public ServerConfigurationPropertyValidation Validation { get; set; }

        #region Commands

        public ICommand ExecuteActionCommand => _executeActionCommand ??= new RelayCommand(() =>
        {
            Action.Action?.Invoke(this);
        });
        private RelayCommand _executeActionCommand;

        #endregion

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
