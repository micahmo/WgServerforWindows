using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using SharpConfig;
using WgAPI;
using WgAPI.Commands;
using WgServerforWindows.Cli.Options;
using WgServerforWindows.Controls;
using WgServerforWindows.Properties;
using System.Collections.Generic;
using System.Linq;

namespace WgServerforWindows.Models
{
    public class ServerStatusPrerequisite : PrerequisiteItem
    {
        #region PrerequisiteItem members

        public ServerStatusPrerequisite() : base
        (
            title: Resources.ServerStatusTitle,
            successMessage: Resources.ServerStatusSuccessMessage,
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
            CliWrap.Cli.Wrap(Path.Combine(AppContext.BaseDirectory, "WgServerforWindows.exe"))
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

        public string ServerStatus
        {
            get
            {
                Dictionary<string, string> clients = new();
                try
                {
                    // First, load all of the clients, so we can associate peer IDs.
                    ClientConfigurationList clientConfigurations = new ClientConfigurationList();
                    List<ClientConfiguration> clientConfigurationsFromFile = new List<ClientConfiguration>();
                    foreach (string clientConfigurationFile in Directory.GetFiles(ClientConfigurationsPrerequisite.ClientDataDirectory, "*.conf"))
                    {
                        clientConfigurationsFromFile.Add(new ClientConfiguration(clientConfigurations).Load<ClientConfiguration>(Configuration.LoadFromFile(clientConfigurationFile)));
                    }

                    clients = clientConfigurationsFromFile.ToDictionary(c => c.PublicKeyProperty.Value, c => c.Name);
                }
                catch
                {
                    // Ignore if there's any problem with this
                }

                // Get the output of the status command
                string statusOutput = new WireGuardExe().ExecuteCommand(new ShowCommand(ServerConfigurationPrerequisite.WireGuardServerInterfaceName));

                // Iterate through the output and correlate peer IDs to names
                StringBuilder result = new StringBuilder();
                foreach (string line in statusOutput.Split(Environment.NewLine))
                {
                    Match match = _peerRegex.Match(line);
                    if (match.Success && clients.ContainsKey(match.Groups[1].Value))
                    {
                        result.AppendLine(_peerRegex.Replace(line, $"peer: {clients[match.Groups[1].Value]} ({match.Groups[1].Value})"));
                    }
                    else
                    {
                        result.AppendLine(line);
                    }
                }

                return result.ToString();
            }
        }

        public bool UpdateLive
        {
            get => _updateLive;
            set => Set(nameof(UpdateLive), ref _updateLive, value);
        }
        private bool _updateLive = true;

        #endregion

        #region Private fields

        private readonly DispatcherTimer _updateTimer;
        private readonly Regex _peerRegex = new Regex(@"peer:\s([^\s]+)", RegexOptions.Compiled);

        #endregion
    }
}
