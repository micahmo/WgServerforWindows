using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using WireGuardAPI;

namespace WireGuardServerForWindows.Models
{
    public class WireGuardExePrerequisite : PrerequisiteItem
    {
        public WireGuardExePrerequisite() : base
        (
            title: "WireGuard.exe",
            successMessage: "Found WireGuard.exe in PATH.",
            errorMessage: "WireGuard.exe is not found in the PATH. It must be downloaded and installed.",
            resolveText: "Download and install WireGuard",
            configureText: "Uninstall WireGuard"
        )
        {
        }

        public override bool Fulfilled
        {
            get
            {
                _wireGuardExe ??= new WireGuardExe();
                return _wireGuardExe.Exists;
            }
        }

        public override void Resolve()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            
            string downloadPath = Path.Combine(Path.GetTempPath(), "wireguard.exe");
            new WebClient().DownloadFile(wireGuardExeDownload, downloadPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = downloadPath,
                Verb = "runas", // For elevation
                UseShellExecute = true // Must be true to use "runas"
            });

            Task.Run(WaitForWireGuardProcess);

            Mouse.OverrideCursor = null;
        }

        public override void Configure()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            _wireGuardExe.ExecuteCommand(new UninstallCommand());
            Refresh();

            Mouse.OverrideCursor = null;
        }

        private async void WaitForWireGuardProcess()
        {
            while (!Fulfilled)
            {
                await Task.Delay((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
            }

            Refresh();
        }

        private readonly string wireGuardExeDownload = @"https://download.wireguard.com/windows-client/wireguard-installer.exe";
        private WireGuardExe _wireGuardExe;
    }
}
