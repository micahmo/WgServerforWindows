using System;
using System.IO;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ChangeClientConfigDirectorySubCommand : PrerequisiteItem
    {
        public ChangeClientConfigDirectorySubCommand() : base
        (
            title: string.Empty,
            successMessage: string.Empty,
            errorMessage: string.Empty,
            resolveText: string.Empty,
            configureText: Resources.ChangeClientConfigDirectoryConfigureText
        )
        {
            AppSettings.Instance.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(AppSettings.Instance.CustomClientConfigDirectory))
                {
                    RaisePropertyChanged(nameof(SuccessMessage));
                }
            };
        }

        #region PrerequisiteItem members

        public override string SuccessMessage
        {
            get => string.Format(Resources.ChangeClientConfigDirectorySuccessMessage, ClientConfigurationsPrerequisite.ClientConfigDirectory);
            set { }
        }

        public override void Configure()
        {
            using CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = ClientConfigurationsPrerequisite.ClientConfigDirectory
            };

            if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok
                && Directory.Exists(commonOpenFileDialog.FileName)
                && !commonOpenFileDialog.FileName.Equals(ClientConfigurationsPrerequisite.ClientConfigDirectory, StringComparison.OrdinalIgnoreCase))
            {
                WaitCursor.SetOverrideCursor(Cursors.Wait);

                if (Directory.Exists(ClientConfigurationsPrerequisite.ClientWGDirectory))
                {
                    Directory.Move(ClientConfigurationsPrerequisite.ClientWGDirectory, Path.Combine(commonOpenFileDialog.FileName, Path.GetFileName(ClientConfigurationsPrerequisite.ClientWGDirectory)));
                }

                if (Directory.Exists(ClientConfigurationsPrerequisite.ClientDataDirectory))
                {
                    Directory.Move(ClientConfigurationsPrerequisite.ClientDataDirectory, Path.Combine(commonOpenFileDialog.FileName, Path.GetFileName(ClientConfigurationsPrerequisite.ClientDataDirectory)));
                }

                AppSettings.Instance.CustomClientConfigDirectory = commonOpenFileDialog.FileName;
                AppSettings.Instance.Save();

                WaitCursor.SetOverrideCursor(null);
            }
        }

        #endregion
    }
}
