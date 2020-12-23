using System.Linq;
using Microsoft.Win32;

namespace WireGuardAPI
{
    public class UninstallCommand : WireGuardCommand
    {
        public UninstallCommand() : base(string.Empty, WhichExe.Custom, Mode.None)
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
