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
            // Upon construction, make sure that we can find the exe in the path.
            _path = GetPath();
        }

        #endregion

        #region Private methods

        private string GetPath()
        {
            string result = default;

            // Must use EnvironmentVariableTarget.Machine so that we always get the latest variables, even if they change after our process starts.
            if (Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Machine) is string pathEnv)
            {
                foreach (string path in pathEnv.Split(';'))
                {
                    string wireGuardExePath = Path.Combine(path, "wireguard.exe");
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

        // Re-evaluate every time, in case it gets installed (or UNinstalled) after we're instantiated
        public bool Exists => string.IsNullOrEmpty(_path = GetPath()) == false;

        #endregion
        
        #region Private fields

        private string _path;

        #endregion

        #region Public methods

        public void ExecuteCommand(WireGuardCommand command)
        {
            switch (command.WhichExe)
            {
                case WhichExe.WireGuardExe:
                    break;
                case WhichExe.WGExe:
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
        }

        #endregion
    }
}
