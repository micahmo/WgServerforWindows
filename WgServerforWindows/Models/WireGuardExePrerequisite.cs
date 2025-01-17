using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Flurl.Http;
using WgAPI;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class WireGuardExePrerequisite : PrerequisiteItem
    {
        public WireGuardExePrerequisite() : base
        (
            title: Resources.WireGuardExe,
            successMessage: Resources.WireGuardExeFound,
            errorMessage: Resources.WireGuardExeNotFound,
            resolveText: Resources.InstallWireGuard,
            configureText: Resources.UninstallWireGuard
        )
        {
        }

        public override BooleanTimeCachedProperty Fulfilled => _fulfilled ??= new BooleanTimeCachedProperty(TimeSpan.FromSeconds(1), () =>
        {
            _wireGuardExe ??= new WireGuardExe();
            return _wireGuardExe.Exists;
        });
        private BooleanTimeCachedProperty _fulfilled;

        public override void Resolve()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            string downloadPath = Path.GetTempPath();
            string downloadFileName = "wireguard.exe";
            wireGuardExeDownload.DownloadFileAsync(downloadPath, downloadFileName).GetAwaiter().GetResult();
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(downloadPath, downloadFileName),
                Verb = "runas", // For elevation
                UseShellExecute = true // Must be true to use "runas"
            });

            Task.Run(WaitForFulfilled);

            WaitCursor.SetOverrideCursor(null);
        }

        public override void Configure()
        {
            WaitCursor.SetOverrideCursor(Cursors.Wait);

            _wireGuardExe.ExecuteCommand(new UninstallCommand());
            Refresh();

            WaitCursor.SetOverrideCursor(null);
        }

        private readonly string wireGuardExeDownload = @"https://download.wireguard.com/windows-client/wireguard-installer.exe";
        private WireGuardExe _wireGuardExe;
    }
}
