using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Input;
using SharpConfig;
using WgAPI;
using WgAPI.Commands;
using WgServerforWindows.Controls;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class TunnelServicePrerequisite : PrerequisiteItem
    {
        public TunnelServicePrerequisite() : this(new TunnelServiceNameSubCommand())
        {
        }

        public TunnelServicePrerequisite(TunnelServiceNameSubCommand tunnelServiceNameSubCommand) : base
        (
            title: Resources.TunnelService,
            successMessage: Resources.TunnelServiceInstalled,
            errorMessage: Resources.TunnelServiceNotInstalled,
            resolveText: Resources.InstallTunnelService,
            configureText: Resources.UninstallTunnelService
        )
        {
            SubCommands.Add(tunnelServiceNameSubCommand);
        }

        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Any(nic => nic.Name == GlobalAppSettings.Instance.TunnelServiceName);
        });
        private BooleanTimeCachedProperty _fulfilled;

        public override async void Resolve()
        {
            try
            {
                // Load the server config and check the listen port
                ServerConfiguration serverConfiguration = new ServerConfiguration().Load<ServerConfiguration>(Configuration.LoadFromFile(ServerConfigurationPrerequisite.ServerDataPath));
                string listenPort = serverConfiguration.ListenPortProperty.Value;

                if (int.TryParse(listenPort, out int listenPortInt))
                {
                    IPEndPoint[] tcpEndPoints = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                    IPEndPoint[] udpEndPoints = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();

                    bool anyTcpListener = tcpEndPoints.Any(endpoint => endpoint.Port == listenPortInt);
                    bool anyUdpListener = udpEndPoints.Any(endpoint => endpoint.Port == listenPortInt);

                    if (anyUdpListener)
                    {
                        // Give the user strong warning about UDP listener
                        bool canceled = false;
                        UnhandledErrorWindow portWarningDialog = new UnhandledErrorWindow();
                        portWarningDialog.DataContext = new UnhandledErrorWindowModel
                        {
                            Title = Resources.PotentialPortConflict,
                            Text = string.Format(Resources.UDPPortConflictMessage, listenPort),
                            SecondaryButtonText = Resources.Cancel,
                            SecondaryButtonAction = () =>
                            {
                                canceled = true;
                                portWarningDialog.Close();
                            }
                        };
                        portWarningDialog.ShowDialog();

                        if (canceled)
                        {
                            return;
                        }
                    }
                    else if (anyTcpListener)
                    {
                        // Give the user less strong warning about TCP listener
                        bool canceled = false;
                        UnhandledErrorWindow portWarningDialog = new UnhandledErrorWindow();
                        portWarningDialog.DataContext = new UnhandledErrorWindowModel
                        {
                            Title = Resources.PotentialPortConflict,
                            Text = string.Format(Resources.TCPPortConflictMessage, listenPort),
                            SecondaryButtonText = Resources.Cancel,
                            SecondaryButtonAction = () =>
                            {
                                canceled = true;
                                portWarningDialog.Close();
                            }
                        };
                        portWarningDialog.ShowDialog();

                        if (canceled)
                        {
                            return;
                        }
                    }
                }
            }
            catch
            {
                // If we can't verify the listen port, it's ok.
            }

            WaitCursor.SetOverrideCursor(Cursors.Wait);

            using (TemporaryFile temporaryFile = new(ServerConfigurationPrerequisite.ServerWGPath, ServerConfigurationPrerequisite.ServerWGPathWithCustomTunnelName))
            {
                new WireGuardExe().ExecuteCommand(new InstallTunnelServiceCommand(temporaryFile.NewFilePath));
            }
            
            await WaitForFulfilled();
            
            WaitCursor.SetOverrideCursor(null);
        }

        public override async void Configure()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            new WireGuardExe().ExecuteCommand(new UninstallTunnelServiceCommand(GlobalAppSettings.Instance.TunnelServiceName));
            await WaitForFulfilled(false);

            WaitCursor.SetOverrideCursor(null);
        }
    }
}
