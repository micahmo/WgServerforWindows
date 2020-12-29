namespace WireGuardAPI.Commands
{
    public class ShowCommand : WireGuardCommand
    {
        public ShowCommand(string interfaceName) : base
        (
            @switch: "show",
            whichExe: WhichExe.WGExe,
            args: interfaceName
        )
        {
        }
    }
}
