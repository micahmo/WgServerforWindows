using System.IO;
using System.Windows;
using System.Windows.Input;
using CliWrap;
using CliWrap.Buffered;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class PrivateNetworkPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public PrivateNetworkPrerequisite() : base
        (
            title: Resources.PrivateNetworkTitle,
            successMessage: Resources.PrivateNetworkSuccess,
            errorMessage: Resources.PrivateNetworkError,
            resolveText: Resources.PrivateNetworkResolve,
            configureText: Resources.PrivateNetworkConfigure
        )
        {
        }

        public override bool Fulfilled
        {
            get
            {
                bool result = default;

                var cmd = Cli.Wrap("powershell").WithArguments(
                    $"(get-netconnectionprofile -interfacealias {Path.GetFileNameWithoutExtension(ServerConfigurationPrerequisite.ServerWGPath)}).NetworkCategory");
                result = cmd.ExecuteBufferedAsync().Task.Result.StandardOutput.Trim() == "Private";

                return result;
            }
        }

        public override void Resolve()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var result = Cli.Wrap("powershell")
                .WithArguments($"$profile = get-netconnectionprofile -interfacealias {Path.GetFileNameWithoutExtension(ServerConfigurationPrerequisite.ServerWGPath)}; " +
                               "$profile.networkcategory = 'Private'; " +
                               "set-netconnectionprofile -inputobject $profile")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync().Task.Result;

            if (string.IsNullOrEmpty(result.StandardError) == false)
            {
                MessageBox.Show(result.StandardError);
            }

            Refresh();

            Mouse.OverrideCursor = null;
        }

        public override void Configure()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var result = Cli.Wrap("powershell")
                .WithArguments($"$profile = get-netconnectionprofile -interfacealias {Path.GetFileNameWithoutExtension(ServerConfigurationPrerequisite.ServerWGPath)}; " +
                               "$profile.networkcategory = 'Public'; " +
                               "set-netconnectionprofile -inputobject $profile")
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync().Task.Result;

            if (string.IsNullOrEmpty(result.StandardError) == false)
            {
                MessageBox.Show(result.StandardError);
            }

            Refresh();

            Mouse.OverrideCursor = null;
        }

        #endregion
    }
}
