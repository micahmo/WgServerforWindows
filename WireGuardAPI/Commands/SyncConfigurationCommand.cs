namespace WireGuardAPI.Commands
{
    public class SyncConfigurationCommand : WireGuardCommand
    {
        public SyncConfigurationCommand(string interfaceName, string configurationFile) : base("syncconf", WhichExe.WGExe)
        {
            Args = new[] {interfaceName, configurationFile};
            RunAsAdministrator = true;
        }
    }
}
