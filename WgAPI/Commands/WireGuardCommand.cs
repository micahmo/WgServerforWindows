using System.IO;
using System.Text;

namespace WgAPI
{
    public class WireGuardCommand
    {
        public WireGuardCommand(string @switch, WhichExe whichExe, params string[] args)
        {
            Switch = @switch;
            WhichExe = whichExe;
            Args = args;
        }

        public string Switch { get; protected set; }

        public string[] Args { get; protected set; }

        public WhichExe WhichExe { get; protected set; }

        public string StandardInput { get; protected set; } = string.Empty;


        /// <summary>
        /// Remove "WireGuard.exe" / "wg-quick" specific settings which aren't supported by "wg.exe" / "wg".
        /// This is similar to the "wg-quick strip" command under Linux, used for example in: wg syncconf wg0 &lt;(wg-quick strip wg0)
        /// Make sure to pass a temporary file path to avoid overwriting the original configuration.
        /// </summary>
        /// <param name="temporaryFilePath"></param>
        public static void StripConfigFile(string temporaryFilePath)
        {
            var lines = File.ReadAllLines(temporaryFilePath);
            var section = "";
            var sb = new StringBuilder((int)new FileInfo(temporaryFilePath).Length);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith('[') && line.EndsWith(']'))
                    section = line.Substring(1, line.Length - 2);

                // "[Interface] Address=..." isn't supported by wg (i.e.: wg syncconf)
                if (section == "Interface" && line.Replace(" ", "").StartsWith("Address="))
                    continue;

                sb.AppendLine(rawLine);
            }

            File.WriteAllText(temporaryFilePath, sb.ToString());
        }
    }

    public enum WhichExe
    {
        WireGuardExe,
        WGExe,
        Custom,
        CustomInteractive
    }
}
