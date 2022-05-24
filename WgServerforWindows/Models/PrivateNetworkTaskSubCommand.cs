using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32.TaskScheduler;
using WgServerforWindows.Cli.Options;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class PrivateNetworkTaskSubCommand : PrerequisiteItem
    {
        public PrivateNetworkTaskSubCommand() : base
        (
            string.Empty,
            successMessage: Resources.PrivateNetworkTaskSubCommandSuccessMessage,
            errorMessage: Resources.PrivateNetworkTaskSubCommandErrorMessage,
            resolveText: Resources.PrivateNetworkTaskSubCommandResolveText,
            configureText: Resources.PrivateNetworkTaskSubCommandConfigureText
        )
        {
        }

        #region PrerequisiteItem members

        /// <inheritdoc/>
        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
            TaskService.Instance.FindTask(_privateNetworkTaskUniqueName) is { Enabled: true } task
            && task.Definition.Triggers.FirstOrDefault() is BootTrigger
            && task.Definition.Actions.FirstOrDefault() is ExecAction action
            && action.Path == Path.Combine(AppContext.BaseDirectory, "ws4w.exe")
            && action.Arguments.StartsWith(typeof(PrivateNetworkCommand).GetVerb()));
        private BooleanTimeCachedProperty _fulfilled;

        /// <inheritdoc/>
        public override void Resolve()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            // Create/update a Scheduled Task that sets the Private network category on boot.
            TaskDefinition td = TaskService.Instance.NewTask();
            td.Actions.Add(new ExecAction(Path.Combine(AppContext.BaseDirectory, "ws4w.exe"), typeof(PrivateNetworkCommand).GetVerb()));
            td.Triggers.Add(new BootTrigger());
            TaskService.Instance.RootFolder.RegisterTaskDefinition(_privateNetworkTaskUniqueName, td, TaskCreation.CreateOrUpdate, "SYSTEM", null, TaskLogonType.ServiceAccount);

            Refresh();

            WaitCursor.SetOverrideCursor(null);
        }

        public override void Configure()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            // Disable the task
            if (TaskService.Instance.FindTask(_privateNetworkTaskUniqueName) is { } task)
            {
                task.Enabled = false;
            }

            Refresh();

            WaitCursor.SetOverrideCursor(null);
        }

        #endregion

        #region Private fields

        private readonly string _privateNetworkTaskUniqueName = "WS4W Private Network (bc87228e-afdb-4815-8786-b5934bcf53e6)";

        #endregion
    }
}
