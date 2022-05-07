using CommandLine;
using WireGuardServerForWindows.Cli.Options.Properties;

namespace WireGuardServerForWindows.Cli.Options
{
    [Verb("privatenetwork", HelpText = nameof(Resources.PrivateNetworkHelpText), ResourceType = typeof(Resources))]
    public class PrivateNetworkCommand
    {
        // No options for this command
    }
}