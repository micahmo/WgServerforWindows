using CommandLine;
using WireGuardServerForWindows.Cli.Options.Properties;

namespace WireGuardServerForWindows.Cli.Options
{
    [Verb("setnetipaddress", HelpText = nameof(Resources.SetNetIpAddressHelpText), ResourceType = typeof(Resources))]
    public class SetNetIpAddressCommand
    {
        [Option("serverdatapath", HelpText = nameof(Resources.SetNetIpAddressCommandServerDataPathHelpText), ResourceType = typeof(Resources))]
        public string ServerDataPath { get; set; }
    }
}
