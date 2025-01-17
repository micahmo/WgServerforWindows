using System;
using System.ComponentModel;
using CliWrap;
using CliWrap.Buffered;
using CommandLine;
using CommandLine.Text;
using WgServerforWindows.Cli.Options;
using WgServerforWindows.Cli.Properties;

namespace WgServerforWindows.Cli
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
                    var result = CliWrap.Cli.Wrap("WgServerforWindows.exe")
                        .WithArguments(args)
                        .WithValidation(CommandResultValidation.None)
                        .ExecuteBufferedAsync().GetAwaiter().GetResult();

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
