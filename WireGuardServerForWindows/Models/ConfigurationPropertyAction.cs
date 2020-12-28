using System;
using GalaSoft.MvvmLight;

namespace WireGuardServerForWindows.Models
{
    public class ConfigurationPropertyAction : ObservableObject
    {
        public ConfigurationPropertyAction(ConfigurationProperty dependentProperty = null)
        {
            _dependentProperty = dependentProperty;

            if (_dependentProperty is { })
            {
                _dependentProperty.PropertyChanged += (_, __) =>
                {
                    RaisePropertyChanged(nameof(DependencySatisfied));
                };
            }
        }

        public string Name { get; set; }

        public bool DependencySatisfied
        {
            get
            {
                bool result = true;

                if (_dependentProperty is { } && DependencySatisfiedFunc is { })
                {
                    result = DependencySatisfiedFunc(_dependentProperty);
                }

                return result;
            }
        }

        public Func<ConfigurationProperty, bool> DependencySatisfiedFunc { get; set; }

        public Action<ConfigurationBase, ConfigurationProperty> Action { get; set; }

        private readonly ConfigurationProperty _dependentProperty;
    }

    public class ConfigurationPropertyValidation
    {
        public Func<ConfigurationProperty, string> Validate { get; set; }
    }
}
