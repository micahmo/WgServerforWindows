using CommandLine;
using WgServerforWindows.Cli.Options.Properties;

namespace WgServerforWindows.Cli.Options
{
    [Verb("restartinternetsharing", HelpText = nameof(Resources.RestartInternetSharingHelpText), ResourceType = typeof(Resources))]
    public class RestartInternetSharingCommand
    {
        [Option("network", HelpText = nameof(Resources.RestartInternetSharingNetworkHelpText), ResourceType = typeof(Resources))]
        public string NetworkToShare { get; set; }
    }
}
