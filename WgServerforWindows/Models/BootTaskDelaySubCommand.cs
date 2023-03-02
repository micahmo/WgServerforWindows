using System;
using WgServerforWindows.Controls;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class BootTaskDelaySubCommand : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public BootTaskDelaySubCommand() : base
        (
            title: string.Empty,
            successMessage: Resources.BootTaskDelaySuccess,
            errorMessage: string.Empty,
            resolveText: string.Empty,
            configureText: Resources.BootTaskDelayConfigure
        )
        {
        }

        public override BooleanTimeCachedProperty Fulfilled { get; } = new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () => true);

        public override void Resolve()
        {
            throw new NotImplementedException();
        }

        public override void Configure()
        {
            DateTime backingObject = new DateTime(1, 1, 1,
                GlobalAppSettings.Instance.BootTaskDelay.Hours,
                GlobalAppSettings.Instance.BootTaskDelay.Minutes,
                GlobalAppSettings.Instance.BootTaskDelay.Seconds);

            var selectionWindowModel = new SelectionWindowModel<DateTime>
            {
                Title = Resources.BootDelaySelectionTitle,
                Text = Resources.BootDelaySelectionText,
                SelectedItem = new SelectionItem<DateTime> { BackingObject = backingObject },
                IsList = false,
                IsTimeSpan = true
            };

            new SelectionWindow
            {
                DataContext = selectionWindowModel
            }.ShowDialog();

            if (selectionWindowModel.DialogResult == true)
            {
                var timeSpan = new TimeSpan(
                    selectionWindowModel.SelectedItem.BackingObject.Hour,
                    selectionWindowModel.SelectedItem.BackingObject.Minute,
                    selectionWindowModel.SelectedItem.BackingObject.Second);

                GlobalAppSettings.Instance.BootTaskDelay = timeSpan;
                GlobalAppSettings.Instance.Save();
            }
        }

        #endregion
    }
}
