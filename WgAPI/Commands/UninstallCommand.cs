using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace WgAPI
{
    [SupportedOSPlatform("windows")] // Suppress warnings about registry not working on all platforms
    public class UninstallCommand : WireGuardCommand
    {
        public UninstallCommand() : base(string.Empty, WhichExe.Custom)
        {
            Args = FindUninstallCommand().Split();
        }

        // Inspired by https://stackoverflow.com/a/7206715/4206279
        private string FindUninstallCommand()
        {
            string result = default;

            RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
            foreach (string installedApplication in uninstallKey?.GetSubKeyNames() ?? Enumerable.Empty<string>())
            {
                RegistryKey installedApplicationKey = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{installedApplication}");
                if (installedApplicationKey?.GetValue("DisplayName")?.ToString() == "WireGuard")
                {
                    result = installedApplicationKey.GetValue("UninstallString").ToString();
                }
            }

            return result;
        }
    }
}
