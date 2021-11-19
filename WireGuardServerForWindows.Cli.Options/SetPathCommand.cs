using CommandLine;
using WireGuardServerForWindows.Cli.Options.Properties;

namespace WireGuardServerForWindows.Cli.Options
{
    [Verb("setpath", HelpText = nameof(Resources.SetPathHelpText), ResourceType = typeof(Resources))]
    public class SetPathCommand
    {
        // No options for this command
    }
}