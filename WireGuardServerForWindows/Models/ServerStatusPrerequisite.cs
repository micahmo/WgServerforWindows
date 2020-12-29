using WireGuardServerForWindows.Controls;

namespace WireGuardServerForWindows.Models
{
    public class ServerStatusPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public ServerStatusPrerequisite() : base
        (
            title: "View Server Status",
            successMessage: string.Empty,
            errorMessage: string.Empty,
            resolveText: string.Empty,
            configureText: "View"
        )
        {
        }

        public override bool Fulfilled => true;

        public override void Resolve()
        {
            throw new System.NotImplementedException();
        }

        public override void Configure()
        {
            new ServerStatusWindow().ShowDialog();
        }

        #endregion
    }
}
