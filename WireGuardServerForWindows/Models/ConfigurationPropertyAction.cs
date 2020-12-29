using System;
using GalaSoft.MvvmLight;

namespace WireGuardServerForWindows.Models
{
    public class ConfigurationPropertyAction : ObservableObject
    {
        public ConfigurationPropertyAction() { }

        public string Name { get; set; }

        public string Description { get; set; }

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

        public Func<ConfigurationProperty, bool> DependencySatisfiedFunc { get; set; }

        public Action<ConfigurationBase, ConfigurationProperty> Action { get; set; }
    }

    public class ConfigurationPropertyValidation
    {
        public Func<ConfigurationProperty, string> Validate { get; set; }
    }
}
