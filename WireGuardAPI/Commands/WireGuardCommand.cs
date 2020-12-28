namespace WireGuardAPI
{
    public abstract class WireGuardCommand
    {
        protected WireGuardCommand(string @switch, WhichExe whichExe, params string[] args)
        {
            Switch = @switch;
            WhichExe = whichExe;
            Args = args;
        }

        public string Switch { get; protected set; }

        public string[] Args { get; protected set; }

        public WhichExe WhichExe { get; protected set; }

        public bool RunAsAdministrator { get; protected set; }

        public string StandardInput { get; protected set; }
    }

    public enum WhichExe
    {
        WireGuardExe,
        WGExe,
        Custom,
    }
}
