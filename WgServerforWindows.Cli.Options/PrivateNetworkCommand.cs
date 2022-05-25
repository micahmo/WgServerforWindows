using CommandLine;
using WgServerforWindows.Cli.Options.Properties;

namespace WgServerforWindows.Cli.Options
{
    [Verb("privatenetwork", HelpText = nameof(Resources.PrivateNetworkHelpText), ResourceType = typeof(Resources))]
    public class PrivateNetworkCommand
    {
        // No options for this command
    }
}