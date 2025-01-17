using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Management;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using WgServerforWindows.Cli.Options;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
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

            try
            {
                // First, check whether the service is set to start automatically
                if (GetICSService() is { } service)
                {
                    bool isAutomatic = service.Properties["StartMode"].Value as string == "Automatic" ||
                                       service.Properties["StartMode"].Value as string == "Auto";

                    if (isAutomatic)
                    {
                        // Now check whether the special registry entry exists
                        // If good, result is true
                        if (GetRegistryKeyValue() is { } value && value == 1)
                        {
                            // Finally, verify that the task exists and that all of the parameters are correct.
                            result = TaskService.Instance.FindTask(RestartInternetSharingTaskUniqueName) is { Enabled: true } task
                                     && task.Definition.Triggers.FirstOrDefault() is BootTrigger
                                     && task.Definition.Actions.FirstOrDefault() is ExecAction action
                                     && action.Path == Path.Combine(AppContext.BaseDirectory, "ws4w.exe")
                                     && action.Arguments == typeof(RestartInternetSharingCommand).GetVerb();
                        }
                    }
                }
            }
            catch 
            {
                // If there's any error getting the ICS Service, then it's clearly not resolved, so return false.
            }

            return result;
        });
        private BooleanTimeCachedProperty _fulfilled;

        public override void Resolve()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            if (GetICSService() is { } service)
            {
                var parameters = service.GetMethodParameters("ChangeStartMode");
                parameters["StartMode"] = "Automatic";
                service.InvokeMethod("ChangeStartMode", parameters, null);
            }

            SetRegistryKeyValue(1);

            // Create/update a Scheduled Task that disables/enables internet sharing on boot.
            TaskDefinition td = TaskService.Instance.NewTask();
            td.Actions.Add(new ExecAction(Path.Combine(AppContext.BaseDirectory, "ws4w.exe"), typeof(RestartInternetSharingCommand).GetVerb()));
            td.Triggers.Add(new BootTrigger { Delay = GlobalAppSettings.Instance.BootTaskDelay });
            TaskService.Instance.RootFolder.RegisterTaskDefinition(RestartInternetSharingTaskUniqueName, td, TaskCreation.CreateOrUpdate, "SYSTEM", null, TaskLogonType.ServiceAccount);

            Refresh();

            WaitCursor.SetOverrideCursor(null);
        }

        public override void Configure()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            if (GetICSService() is { } service)
            {
                var parameters = service.GetMethodParameters("ChangeStartMode");
                parameters["StartMode"] = "Manual";
                service.InvokeMethod("ChangeStartMode", parameters, null);
            }

            SetRegistryKeyValue(0);

            if (TaskService.Instance.FindTask(RestartInternetSharingTaskUniqueName) is { } task)
            {
                task.Enabled = false;
            }

            Refresh();

            WaitCursor.SetOverrideCursor(null);
        }

        public override string Category => Resources.InternetConnectionSharing;

        #endregion

        #region Private methods

        private ManagementObject GetICSService()
        {
            ManagementObjectSearcher managementObjectSearcher =  new ManagementObjectSearcher("root\\cimv2", "select * from win32_service where name = 'SharedAccess'");
            return managementObjectSearcher.Get().OfType<ManagementObject>().FirstOrDefault();
        }

        private int? GetRegistryKeyValue()
        {
            RegistryKey sharedAccessKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\SharedAccess");
            return sharedAccessKey?.GetValue("EnableRebootPersistConnection") as int?;
        }

        private void SetRegistryKeyValue(int value)
        {
            RegistryKey sharedAccessKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\SharedAccess", writable: true)
                                          ?? Registry.LocalMachine.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\SharedAccess", writable: true);

            if (sharedAccessKey is null)
            {
                throw new Exception("There was an error setting the SharedAccess registry key.");
            }

            sharedAccessKey.SetValue("EnableRebootPersistConnection", value);
        }

        #endregion

        #region Private fields

        private readonly string RestartInternetSharingTaskUniqueName = "WS4W Restart Internet Sharing (b17f2530-acc7-42d6-ad05-ab57b923356f)";

        #endregion
    }
}
