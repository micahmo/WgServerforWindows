using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommandLine;
using WireGuardServerForWindows.Cli.Options;
using WireGuardServerForWindows.Controls;
using WireGuardServerForWindows.Models;
using SplashScreen = WireGuardServerForWindows.Controls.SplashScreen;

namespace WireGuardServerForWindows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += Application_DispatcherUnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Any())
            {
                // First, handle UI-related args
                var uiArgsParsed = Parser.Default.ParseArguments<StatusCommand, object>(e.Args)
                    .WithParsed<StatusCommand>(Status);

                if (uiArgsParsed.Tag != ParserResultType.Parsed)
                {
                    // Otherwise, try non-UI args.

                    // We don't want to handle Dispatcher exceptions in this scenario, since we are UI-less
                    DispatcherUnhandledException -= Application_DispatcherUnhandledException;

                    Parser.Default.ParseArguments<RestartInternetSharingCommand, SetPathCommand, SetNetIpAddressCommand>(e.Args)
                        .WithParsed<RestartInternetSharingCommand>(RestartInternetSharing)
                        .WithParsed<SetPathCommand>(SetPath)
                        .WithParsed<SetNetIpAddressCommand>(SetNetIpAddress);

                    // Don't proceed to GUI if started with command-line args
                    Environment.Exit(0);
                }
            }
            else
            {
                // Otherwise, this is a normal Windowed startup.

                // First, see if we're already running with no args. If so, focus that instance.
                foreach (Process existingProcess in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Where(p => p.Id != Process.GetCurrentProcess().Id))
                {
                    // Get the process's command-line args to see if it's also a normal windowed instance.
                    ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(@"root/cimv2", $"select CommandLine from Win32_Process where ProcessId = '{existingProcess.Id}'");
                    foreach (var ws4wInstance in managementObjectSearcher.Get().OfType<ManagementObject>())
                    {
                        if (ws4wInstance.GetPropertyValue("CommandLine")?.ToString() is { } commandLine)
                        {
                            string arguments = commandLine.Substring(commandLine.LastIndexOf('"') + 2);
                            if (string.IsNullOrEmpty(arguments))
                            {
                                SetForegroundWindow(existingProcess.MainWindowHandle);
                                Environment.Exit(0);
                            }
                        }
                    }
                }

                // Finally, do a normal startup.
                new SplashScreen().Show();
            }
        }

        private static void RestartInternetSharing(RestartInternetSharingCommand o)
        {
            var internetSharingPrerequisite = new InternetSharingPrerequisite();
            string networkToShare = o.NetworkToShare;

            if (string.IsNullOrEmpty(networkToShare))
            {
                // No network specified for re-sharing, retrieve the one already shared.
                List<string> sharedNetworks = internetSharingPrerequisite.GetSharedNetworks();
                networkToShare = sharedNetworks.FirstOrDefault();

                if (string.IsNullOrEmpty(networkToShare))
                {
                    Console.WriteLine(WireGuardServerForWindows.Properties.Resources.CannotRestartInternetSharingNoNetwork);
                    Environment.Exit(1);
                }
                else if (sharedNetworks.Skip(1).Any())
                {
                    Console.WriteLine(WireGuardServerForWindows.Properties.Resources.CannotRestartInternetSharingMultipleNetworks);
                    Environment.Exit(1);
                }
            }

            if (internetSharingPrerequisite.Fulfilled)
            {
                // Internet sharing is already enabled. Disable it, first.
                Console.WriteLine(WireGuardServerForWindows.Properties.Resources.DisablingInternetSharing);
                internetSharingPrerequisite.Configure();
            }

            // Now enable it.
            Console.WriteLine(WireGuardServerForWindows.Properties.Resources.EnablingInternetSharing, networkToShare);
            internetSharingPrerequisite.Resolve(networkToShare);

            int result = internetSharingPrerequisite.Fulfilled ? 0 : 1;

            Environment.Exit(result);
        }

        private static void SetPath(SetPathCommand o)
        {
            string pathEnvVar = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);

            if (string.IsNullOrEmpty(pathEnvVar))
            {
                Console.WriteLine(Cli.Options.Properties.Resources.CantLoadPath);
                Environment.Exit(1);
            }

            string pwd = AppContext.BaseDirectory;

            if (string.IsNullOrEmpty(pwd))
            {
                Console.WriteLine(Cli.Options.Properties.Resources.CantLoadPwd);
                Environment.Exit(1);
            }

            if (pathEnvVar.Contains(pwd) == false)
            {
                pathEnvVar = $"{pathEnvVar};{pwd}";
                Environment.SetEnvironmentVariable("PATH", pathEnvVar, EnvironmentVariableTarget.Machine);
                Console.WriteLine(Cli.Options.Properties.Resources.AddedPwdToPath, pwd);
            }
            else
            {
                Console.WriteLine(Cli.Options.Properties.Resources.FoundPwdInPath, pwd);
            }
        }

        public static void SetNetIpAddress(SetNetIpAddressCommand o)
        {
            Thread.Sleep(TimeSpan.FromSeconds(10));
            new NewNetNatPrerequisite().Resolve(o.ServerDataPath);
        }

        public static void Status(StatusCommand o)
        {
            // Check if the status window is already showing.
            foreach (Process existingProcess in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Where(p => p.Id != Process.GetCurrentProcess().Id))
            {
                // Get the process's command-line args to see if it was run with the "status" flag.
                ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(@"root/cimv2", $"select CommandLine from Win32_Process where ProcessId = '{existingProcess.Id}'");
                foreach (var ws4wInstance in managementObjectSearcher.Get().OfType<ManagementObject>())
                {
                    if (ws4wInstance.GetPropertyValue("CommandLine")?.ToString() is { } commandLine)
                    {
                        string arguments = commandLine.Substring(commandLine.LastIndexOf('"') + 2);
                        if (arguments == typeof(StatusCommand).GetVerb())
                        {
                            SetForegroundWindow(existingProcess.MainWindowHandle);
                            Environment.Exit(0);
                        }
                    }
                }
            }

            // Otherwise, show the status window.
            new ServerStatusPrerequisite().Show();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // In case something was in progress when the error occurred
            WaitCursor.SetOverrideCursor(null);

            Exception realException = e.Exception;
            while (realException.InnerException is { } innerException)
            {
                realException = innerException;
            }

            new UnhandledErrorWindow {DataContext = new UnhandledErrorWindowModel
            {
                Title = WireGuardServerForWindows.Properties.Resources.Error,
                Text = string.Format(WireGuardServerForWindows.Properties.Resources.UnexpectedErrorMessage, realException.Message),
                Exception = e.Exception
            }}.ShowDialog();


            // Don't kill the app
            e.Handled = true;
        }

        #region P/Invoke

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion
    }
}
