using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WireGuardAPI
{
    /// <summary>
    /// Represents the Windows wireguard.exe application that is downloaded and installed from https://www.wireguard.com/install/
    /// </summary>
    public class WireGuardExe
    {
        #region Constructor

        public WireGuardExe()
        {
        }

        #endregion

        #region Private methods

        private string GetPath(string whichExe)
        {
            string result = default;

            // Must use EnvironmentVariableTarget.Machine so that we always get the latest variables, even if they change after our process starts.
            if (Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Machine) is string pathEnv)
            {
                foreach (string path in pathEnv.Split(';'))
                {
                    string wireGuardExePath = Path.Combine(path, whichExe);
                    if (File.Exists(wireGuardExePath))
                    {
                        result = wireGuardExePath;
                        break;
                    }
                }
            }

            return result;
        }

        #endregion

        #region Public properties

        public bool Exists => string.IsNullOrEmpty(GetPath("wireguard.exe")) == false;

        #endregion

        #region Public methods

        public string ExecuteCommand(WireGuardCommand command)
        {
            string result = default;

            switch (command.WhichExe)
            {
                case WhichExe.WireGuardExe:
                    break;
                case WhichExe.WGExe:
                    Process process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = GetPath("wg.exe"),
                            Arguments = string.Join(' ', new[] {command.Switch}.Union(command.Args ?? Enumerable.Empty<string>())),
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                        }
                    };

                    process.Start();
                    process.WaitForExit();

                    result = process.StandardOutput.ReadToEnd().Trim();

                    break;
                case WhichExe.Custom:
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = command.Args[0],
                        Arguments = string.Join(' ', command.Args.Skip(1)),
                        Verb = "runas",
                        UseShellExecute = true,
                    })?.WaitForExit();
                    break;
            }

            return result;
        }

        #endregion
    }
}
