using System.Diagnostics;
using System.Windows;
using Bluegrams.Application;

namespace WireGuardServerForWindows
{
    public class MyUpdateChecker : WpfUpdateChecker
    {
        public MyUpdateChecker(string url, Window owner = null, string identifier = null) : base(url, owner, identifier)
        {
        }

        public override void ShowUpdateDownload(string file)
        {
            // All we have to do is start the installer.
            // It will handle killing this instance of the app and restarting it at the end.
            Process.Start(file);
        }
    }
}