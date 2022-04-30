using System;
using System.IO;
using System.Windows.Threading;
using WireGuardAPI;
using WireGuardAPI.Commands;
using WireGuardServerForWindows.Cli.Options;
using WireGuardServerForWindows.Controls;
using WireGuardServerForWindows.Properties;

namespace WireGuardServerForWindows.Models
{
    public class ServerStatusPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public ServerStatusPrerequisite() : base
        (
            title: Resources.ServerStatusTitle,
            successMessage: string.Empty,
            errorMessage: string.Empty,
            resolveText: string.Empty,
            configureText: Resources.ServerStatusConfigureText
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

        public override BooleanTimeCachedProperty Fulfilled { get; } = new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () => true);

        public override void Resolve()
        {
            throw new NotImplementedException();
        }

        public override void Configure()
        {
            CliWrap.Cli.Wrap(Path.Combine(AppContext.BaseDirectory, "WireGuardServerForWindows.exe"))
                .WithArguments(typeof(StatusCommand).GetVerb())
                .ExecuteAsync();
        }

        public override BooleanTimeCachedProperty IsInformational { get; } = new BooleanTimeCachedProperty(TimeSpan.Zero, () => true);

        #endregion

        #region Public methods

        public void Show()
        {
            _updateTimer.IsEnabled = true;
            new ServerStatusWindow { DataContext = this }.ShowDialog();
            _updateTimer.IsEnabled = false;
        }

        #endregion

        #region Public properties

        public string ServerStatus => new WireGuardExe().ExecuteCommand(new ShowCommand(ServerConfigurationPrerequisite.WireGuardServerInterfaceName));

        public bool UpdateLive
        {
            get => _updateLive;
            set => Set(nameof(UpdateLive), ref _updateLive, value);
        }
        private bool _updateLive = true;

        #endregion

        #region Private fields

        private readonly DispatcherTimer _updateTimer;

        #endregion
    }
}
