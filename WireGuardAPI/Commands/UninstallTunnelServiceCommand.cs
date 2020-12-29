namespace WireGuardAPI.Commands
{
    public class UninstallTunnelServiceCommand : WireGuardCommand
    {
        public UninstallTunnelServiceCommand(string serviceName) : base
        (
            @switch: "/uninstalltunnelservice",
            whichExe: WhichExe.WireGuardExe,
            serviceName
        )
        {
        }
    }
}
