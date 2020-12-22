namespace WireGuardAPI
{
    public abstract class WireGuardCommand
    {
        protected WireGuardCommand(string @switch, WhichExe whichExe, Mode mode, params string[] args)
        {
            Switch = @switch;
            WhichExe = whichExe;
            Mode = mode;
            Args = args;
        }

        public string Switch { get; protected set; }

        public string[] Args { get; protected set; }

        public WhichExe WhichExe { get; protected set; }

        public Mode Mode { get; protected set; }
    }

    public enum WhichExe
    {
        WireGuardExe,
        WGExe,
        Custom,
    }

    public enum Mode
    {
        None,
        CaptureOutput,
        RunAsAdministrator
    }
}
