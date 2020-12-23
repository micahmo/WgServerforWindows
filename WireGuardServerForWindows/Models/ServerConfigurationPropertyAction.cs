using System;

namespace WireGuardServerForWindows.Models
{
    public class ServerConfigurationPropertyAction
    {
        public string Name { get; set; }

        public Action<ServerConfigurationProperty> Action { get; set; }
    }

    public class ServerConfigurationPropertyValidation
    {
        public Func<ServerConfigurationProperty, string> Validate { get; set; }
    }
}
