namespace WireGuardAPI.Commands
{
    public class GeneratePrivateKeyCommand : WireGuardCommand
    {
        public GeneratePrivateKeyCommand() : base
        (
            @switch: "genkey",
            whichExe: WhichExe.WGExe)
        {
        }
    }
}
