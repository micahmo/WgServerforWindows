using System;
using System.ComponentModel;
using CliWrap;
using CliWrap.Buffered;
using CommandLine;
using CommandLine.Text;
using WireGuardServerForWindows.Cli.Options;
using WireGuardServerForWindows.Cli.Properties;

namespace WireGuardServerForWindows.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = Parser.Default.ParseArguments<RestartInternetSharingCommand, SetPathCommand, SetNetIpAddressCommand, PrivateNetworkCommand>(args);

            parser.WithParsed(o =>
            {
                // If it parses successfully, then just pass along to main exe.
                try
                {
                    var result = CliWrap.Cli.Wrap("WireGuardServerForWindows.exe")
                        .WithArguments(args)
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteBufferedAsync().Task.Result;

                    Console.Write(result.StandardOutput);
                    Console.WriteLine(result.ExitCode == 0 ? Resources.CommandSucceeded : Resources.CommandFailed);

                    Environment.Exit(result.ExitCode);
                }
                catch (Exception ex) when (ex is Win32Exception || ex.InnerException is Win32Exception)
                {
                    Console.WriteLine(Resources.MustRunAsAdmin);
                    Environment.Exit(1);
                }
            });

            parser.WithNotParsed(_ => HelpText.AutoBuild(parser));
        }
    }
}
