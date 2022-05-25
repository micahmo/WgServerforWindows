using System;
using System.Globalization;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using WgServerforWindows.Converters;

namespace WgServerforWindows.Models
{
    public class ConfigurationPropertyAction : ObservableObject
    {
        public ConfigurationPropertyAction(ConfigurationBase parentConfiguration) =>
            _parentConfiguration = parentConfiguration;

        public string Name
        {
            get => _name;
            set => Set(nameof(Name), ref _name, value);
        }
        private string _name;

        public string Description
        {
            get => _description ?? new ResourceStringConverter().Convert(Name, typeof(string), null, CultureInfo.CurrentCulture) as string;
            set => _description = value;
        }
        private string _description;

        public ConfigurationProperty DependentProperty
        {
            get => _dependentProperty;
            set
            {
                _dependentProperty = value;

                if (_dependentProperty is { })
                {
                    _dependentProperty.PropertyChanged += (_, __) =>
                    {
                        RaisePropertyChanged(nameof(DependencySatisfied));
                    };
                }
            }
        }
        private ConfigurationProperty _dependentProperty;

        public bool DependencySatisfied => DependencySatisfiedFunc?.Invoke(_dependentProperty) ?? true;

        public Func<ConfigurationProperty, bool> DependencySatisfiedFunc
        {
            get => _dependencySatisfiedFunc;
            set
            {
                _dependencySatisfiedFunc = value;
                RaisePropertyChanged(nameof(DependencySatisfied));
            }
        }
        private Func<ConfigurationProperty, bool> _dependencySatisfiedFunc;

        public Action<ConfigurationBase, ConfigurationProperty> Action { get; set; }

        public ICommand ExecuteActionCommand => _executeActionCommand ??= new RelayCommand(() =>
        {
            Action?.Invoke(_parentConfiguration, null);
        });
        private RelayCommand _executeActionCommand;

        #region Private fields

        private readonly ConfigurationBase _parentConfiguration;

        #endregion
    }

    public class ConfigurationPropertyValidation
    {
        public Func<ConfigurationProperty, string> Validate { get; set; }
    }
}
