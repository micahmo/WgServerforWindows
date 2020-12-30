using System;
using System.IO;
using System.Windows.Threading;
using WireGuardAPI;
using WireGuardAPI.Commands;
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
            _updateTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            _updateTimer.Tick += (_, __) =>
            {
                if (UpdateLive)
                {
                    RaisePropertyChanged(nameof(ServerStatus));
                }
            };
        }

        public override bool Fulfilled => true;

        public override void Resolve()
        {
            throw new System.NotImplementedException();
        }

        public override void Configure()
        {
            _updateTimer.IsEnabled = true;
            new ServerStatusWindow { DataContext = this }.ShowDialog();
            _updateTimer.IsEnabled = false;
        }

        public override bool IsInformational => true;

        #endregion

        public string ServerStatus => new WireGuardExe().ExecuteCommand(new ShowCommand(ServerConfigurationPrerequisite.WireGuardServerInterfaceName));

        public bool UpdateLive
        {
            get => _updateLive;
            set => Set(nameof(UpdateLive), ref _updateLive, value);
        }
        private bool _updateLive = true;

        #region Private fields

        private readonly DispatcherTimer _updateTimer;

        #endregion
    }
}
