using CommandLine;
using WgServerforWindows.Cli.Options.Properties;

namespace WgServerforWindows.Cli.Options
{
    [Verb("setpath", HelpText = nameof(Resources.SetPathHelpText), ResourceType = typeof(Resources))]
    public class SetPathCommand
    {
        // No options for this command
    }
}