using CommandLine;

namespace WgServerforWindows.Cli.Options
{
    /// <summary>
    /// Instructs the application to show the Status window.
    /// Only invokable through the main executable, not the CLI.
    /// </summary>
    [Verb("status")]
    public class StatusCommand
    {
        // No options for this command
    }
}