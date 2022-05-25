namespace WgAPI.Commands
{
    public class InstallTunnelServiceCommand : WireGuardCommand
    {
        public InstallTunnelServiceCommand(string configurationFile) : base
        (
            @switch: "/installtunnelservice",
            whichExe: WhichExe.WireGuardExe,
            configurationFile
        )
        {
        }
    }
}
