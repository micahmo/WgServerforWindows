using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32.TaskScheduler;
using WgServerforWindows.Cli.Options;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class NewNetIpAddressTaskSubCommand : PrerequisiteItem
    {
        public NewNetIpAddressTaskSubCommand() : base
        (
            string.Empty,
            successMessage: Resources.NewNetIpAddressTaskSubCommandSuccessMessage,
            errorMessage: Resources.NewNetIpAddressTaskSubCommandErrorMessage,
            resolveText: Resources.NewNetIpAddressTaskSubCommandResolveText,
            configureText: Resources.NewNetIpAddressTaskSubCommandConfigureText
        )
        {
        }

        #region PrerequisiteItem members

        /// <inheritdoc/>
        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
            TaskService.Instance.FindTask(_netIpAddressTaskUniqueName) is { Enabled: true } task
            && task.Definition.Triggers.FirstOrDefault() is BootTrigger
            && task.Definition.Actions.FirstOrDefault() is ExecAction action
            && action.Path == Path.Combine(AppContext.BaseDirectory, "ws4w.exe")
            && action.Arguments.StartsWith(typeof(SetNetIpAddressCommand).GetVerb()));
        private BooleanTimeCachedProperty _fulfilled;

        /// <inheritdoc/>
        public override void Resolve()
        {
            Resolve(default);
        }

        public void Resolve(string serverDataPath)
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            // Create/update a Scheduled Task that sets the NetIPAddress on boot.
            TaskDefinition td = TaskService.Instance.NewTask();
            td.Actions.Add(new ExecAction(Path.Combine(AppContext.BaseDirectory, "ws4w.exe"), $"{typeof(SetNetIpAddressCommand).GetVerb()} --{typeof(SetNetIpAddressCommand).GetOption(nameof(SetNetIpAddressCommand.ServerDataPath))} \"{serverDataPath ?? ServerConfigurationPrerequisite.ServerDataPath}\""));
            td.Triggers.Add(new BootTrigger { Delay = GlobalAppSettings.Instance.BootTaskDelay + TimeSpan.FromSeconds(15) });
            TaskService.Instance.RootFolder.RegisterTaskDefinition(_netIpAddressTaskUniqueName, td, TaskCreation.CreateOrUpdate, "SYSTEM", null, TaskLogonType.ServiceAccount);

            Refresh();

            WaitCursor.SetOverrideCursor(null);
        }

        public override void Configure()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            // Disable the task
            if (TaskService.Instance.FindTask(_netIpAddressTaskUniqueName) is { } task)
            {
                task.Enabled = false;
            }

            Refresh();

            WaitCursor.SetOverrideCursor(null);
        }

        #endregion

        #region Private fields

        private readonly string _netIpAddressTaskUniqueName = "WS4W Set NetIPAddress (1048541f-d027-4a97-842d-ca331c3d03a9)";

        #endregion
    }
}
