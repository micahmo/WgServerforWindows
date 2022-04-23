using System;
using System.IO;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ChangeServerConfigDirectorySubCommand : PrerequisiteItem
    {
        public ChangeServerConfigDirectorySubCommand() : base
        (
            title: string.Empty,
            successMessage: string.Empty,
            errorMessage: string.Empty,
            resolveText: string.Empty,
            configureText: Resources.ChangeServerConfigDirectoryConfigureText
        )
        {
            AppSettings.Instance.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(AppSettings.Instance.CustomServerConfigDirectory))
                {
                    RaisePropertyChanged(nameof(SuccessMessage));
                }
            };
        }

        #region PrerequisiteItem members

        public override string SuccessMessage
        {
            get => string.Format(Resources.ChangeServerConfigDirectorySuccessMessage, ServerConfigurationPrerequisite.ServerConfigDirectory);
            set { }
        }

        public override void Configure()
        {
            using CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = ServerConfigurationPrerequisite.ServerConfigDirectory
            };
            
            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok 
                && Directory.Exists(commonOpenFileDialog.FileName)
                && !commonOpenFileDialog.FileName.Equals(ServerConfigurationPrerequisite.ServerConfigDirectory, StringComparison.OrdinalIgnoreCase))
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (Directory.Exists(ServerConfigurationPrerequisite.ServerWGDirectory))
                {
                    Directory.Move(ServerConfigurationPrerequisite.ServerWGDirectory, Path.Combine(commonOpenFileDialog.FileName, Path.GetFileName(ServerConfigurationPrerequisite.ServerWGDirectory)));
                }

                if (Directory.Exists(ServerConfigurationPrerequisite.ServerDataDirectory))
                {
                    Directory.Move(ServerConfigurationPrerequisite.ServerDataDirectory, Path.Combine(commonOpenFileDialog.FileName, Path.GetFileName(ServerConfigurationPrerequisite.ServerDataDirectory)));
                }

                AppSettings.Instance.CustomServerConfigDirectory = commonOpenFileDialog.FileName;
                AppSettings.Instance.Save();

                Mouse.OverrideCursor = null;
            }
        }

        #endregion
    }
}
