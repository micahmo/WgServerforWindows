using System;
using System.Linq;
using System.Management;
using System.Windows.Input;
using Microsoft.Win32;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class PersistentInternetSharingPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public PersistentInternetSharingPrerequisite() : base
        (
            title: Resources.PersistentInternetSharingTitle,
            successMessage: Resources.PersistentInternetSharingSucecss,
            errorMessage: Resources.PersistentInternetSharingError,
            resolveText: Resources.PersistentInternetSharingResolve,
            configureText: Resources.PersistentInternetSharingDisable
        )
        {
        }

        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
        {
            bool result = false;

            // First, check whether the service is set to start automatically
            if (GetICSService() is { } service)
            {
                bool isAutomatic = service.Properties["StartMode"].Value as string == "Automatic" ||
                                   service.Properties["StartMode"].Value as string == "Auto";

                if (isAutomatic)
                {
                    // Now check whether the special registry entry exists
                    // If good, result is true
                    if (GetRegistryKeyValue() is {} value && value == 1)
                    {
                        result = true;
                    }
                }
            }

            return result;
        });
        private BooleanTimeCachedProperty _fulfilled;

        public override void Resolve()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (GetICSService() is { } service)
            {
                var parameters = service.GetMethodParameters("ChangeStartMode");
                parameters["StartMode"] = "Automatic";
                service.InvokeMethod("ChangeStartMode", parameters, null);
            }

            SetRegistryKeyValue(1);

            Refresh();

            Mouse.OverrideCursor = null;
        }

        public override void Configure()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            if (GetICSService() is { } service)
            {
                var parameters = service.GetMethodParameters("ChangeStartMode");
                parameters["StartMode"] = "Manual";
                service.InvokeMethod("ChangeStartMode", parameters, null);
            }

            SetRegistryKeyValue(0);

            Refresh();

            Mouse.OverrideCursor = null;
        }

        #endregion

        #region Private methods

        private ManagementObject GetICSService()
        {
            ManagementObjectSearcher managementObjectSearcher =  new ManagementObjectSearcher("root\\cimv2", "select * from win32_service where name = 'SharedAccess'");
            return managementObjectSearcher.Get().OfType<ManagementObject>().FirstOrDefault();
        }

        private int? GetRegistryKeyValue()
        {
            var sharedAccessKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\SharedAccess");
            return sharedAccessKey?.GetValue("EnableRebootPersistConnection") as int?;
        }

        private void SetRegistryKeyValue(int value)
        {
            var sharedAccessKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\SharedAccess", writable: true);
            sharedAccessKey?.SetValue("EnableRebootPersistConnection", value);
        }

        #endregion
    }
}
