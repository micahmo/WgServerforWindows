using System;
using WgServerforWindows.Controls;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class SettingsPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public SettingsPrerequisite(BootTaskDelaySubCommand bootTaskDelaySubCommand) : base
        (
            title: string.Empty,
            successMessage: string.Empty,
            errorMessage: string.Empty,
            resolveText: string.Empty,
            configureText: Resources.SettingsConfigure
        )
        {
            SubCommands.Add(bootTaskDelaySubCommand);
        }

        public override BooleanTimeCachedProperty Fulfilled { get; } = new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () => true);

        public override BooleanTimeCachedProperty HasIcon { get; } = new BooleanTimeCachedProperty(TimeSpan.Zero, () => false);

        public override void Resolve()
        {
            throw new NotImplementedException();
        }

        public override void Configure()
        {
            if (Control is PrerequisiteItemControl prerequisiteItemControl)
            {
                prerequisiteItemControl.SplitButtonFulfilled.IsOpen = true;
            }
        }

        public override BooleanTimeCachedProperty IsInformational { get; } = new BooleanTimeCachedProperty(TimeSpan.Zero, () => true);

        #endregion
    }
}
