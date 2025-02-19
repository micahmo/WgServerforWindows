using System.Linq;
using WgServerforWindows.Controls;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class TunnelServiceNameSubCommand : PrerequisiteItem
    {
        public TunnelServiceNameSubCommand() : base(
            title: string.Empty,
            successMessage: string.Empty,
            errorMessage: string.Empty,
            resolveText: string.Empty,
            configureText: Resources.TunnelServiceNameSubCommandConfigureText
        )
        {
        }

        /// <inheritdoc/>
        public override void Configure()
        {
            string backingObject = GlobalAppSettings.Instance.TunnelServiceName;

            var selectionWindowModel = new SelectionWindowModel<string>
            {
                Title = Resources.CustomTunnelServiceNameSelectionTitle,
                Text = Resources.CustomTunnelServiceNameSelectionText,
                SelectedItem = new SelectionItem<string> { BackingObject = backingObject },
                IsString = true,
                MinWidth = 350
            };

            selectionWindowModel.CanSelectFunc = () =>
                !string.IsNullOrWhiteSpace(selectionWindowModel.SelectedItem.BackingObject) &&
                selectionWindowModel.SelectedItem.BackingObject.All(c => char.IsLetterOrDigit(c) || c == '_');

            new SelectionWindow
            {
                DataContext = selectionWindowModel
            }.ShowDialog();

            if (selectionWindowModel.DialogResult == true)
            {
                GlobalAppSettings.Instance.TunnelServiceName = selectionWindowModel.SelectedItem.BackingObject;
                GlobalAppSettings.Instance.Save();

                Refresh();
            }
        }
    }
}
