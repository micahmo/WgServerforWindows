using System.Net;
using SharpConfig;
using WgServerforWindows.Controls;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class NetNatRangeSubCommand : PrerequisiteItem
    {
        public NetNatRangeSubCommand() : base
        (
            title: string.Empty,
            successMessage: Resources.NetNatRangeSubCommandSuccessMessage,
            errorMessage: string.Empty,
            resolveText: string.Empty,
            configureText: Resources.NewNetNatRangeSubCommandConfigureText
        )
        {
        }

        public override void Configure()
        {
            var serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath));

            string networkRange = GlobalAppSettings.Instance.CustomNetNatRange;

            var selectionWindowModel = new SelectionWindowModel<string>
            {
                Title = Resources.NetNatRangeSelectionTitle,
                Text = Resources.NetNatRangeSelectionText,
                SelectedItem = new SelectionItem<string> { BackingObject = networkRange },
                IsList = false,
                IsString = true,
                MinWidth = 350
            };

            selectionWindowModel.CanSelectFunc = () =>
            {
                if (!string.IsNullOrWhiteSpace(selectionWindowModel.SelectedItem.BackingObject))
                {
                    if (!IPNetwork2.TryParse(selectionWindowModel.SelectedItem.BackingObject, out _))
                    {
                        selectionWindowModel.ValidationError = Resources.NetworkAddressValidationError;
                        return false;
                    }
                    // IPNetwork2.TryParse recognizes single IP addresses as CIDR (with 8 mask).
                    // This is not good, because we want an explicit CIDR for the server.
                    // Therefore, if IPNetwork2.TryParse succeeds, and IPAddress.TryParse also succeeds, we have a problem.
                    if (IPAddress.TryParse(selectionWindowModel.SelectedItem.BackingObject, out _))
                    {
                        // This is just a regular address. We want CIDR.
                        selectionWindowModel.ValidationError = Resources.NetworkAddressValidationError;
                        return false;
                    }

                    // Ensure that this range contains the server range
                    IPNetwork2 netNatRange = IPNetwork2.Parse(selectionWindowModel.SelectedItem.BackingObject);
                    IPNetwork2 serverRange = IPNetwork2.Parse(serverConfiguration.AddressProperty.Value);
                    if (!netNatRange.Contains(serverRange))
                    {
                        selectionWindowModel.ValidationError = string.Format(Resources.NetNatRangeValidationError, serverRange);
                        return false;
                    }

                    // It passed, so we're good.
                    selectionWindowModel.ValidationError = null;
                    return true;
                }

                // It's empty, which is fine.
                selectionWindowModel.ValidationError = null;
                return true;
            };

            new SelectionWindow
            {
                DataContext = selectionWindowModel
            }.ShowDialog();

            if (selectionWindowModel.DialogResult == true)
            {
                GlobalAppSettings.Instance.CustomNetNatRange = selectionWindowModel.SelectedItem.BackingObject;
                GlobalAppSettings.Instance.Save();

                Refresh();
            }
        }
    }
}
