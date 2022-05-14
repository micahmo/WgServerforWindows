using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CliWrap;
using CliWrap.Buffered;

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

        private string GetPath(WhichExe whichExe)
        {
            string result = default;

            string fileName = whichExe switch
            {
                WhichExe.WireGuardExe => "wireguard.exe",
                WhichExe.WGExe => "wg.exe",
                // This case should never be hit, since custom exes provide their own name via Args
                _ => default
            };

            // Must use EnvironmentVariableTarget.Machine so that we always get the latest variables, even if they change after our process starts.
            if (Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Machine) is { } pathEnv)
            {
                foreach (string path in pathEnv.Split(';'))
                {
                    string wireGuardExePath = Path.Combine(path, fileName);
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

        public bool Exists => string.IsNullOrEmpty(GetPath(WhichExe.WireGuardExe)) == false;

        #endregion

        #region Public methods

        public string ExecuteCommand(WireGuardCommand command)
        {
            return ExecuteCommand(command, out _);
        }

        public string ExecuteCommand(WireGuardCommand command, out int exitCode)
        {
            string result = default;
            exitCode = 1;

            switch (command.WhichExe)
            {
                case WhichExe.WireGuardExe:
                case WhichExe.WGExe:
                    var cmd = command.StandardInput | Cli.Wrap(GetPath(command.WhichExe)).WithArguments(a => a
                        .Add(command.Switch)
                        .Add(command.Args)).WithValidation(CommandResultValidation.None);

                    // For some reason, awaiting this can hang, so this method must do everything synchronously.
                    var bufferedResult = cmd.ExecuteBufferedAsync().Task.Result;
                    result = bufferedResult.StandardOutput.Trim();
                    exitCode = bufferedResult.ExitCode;

                    if (exitCode != 0)
                    {
                        result += bufferedResult.StandardError.Trim();
                    }

                    break;
                case WhichExe.Custom:
                    Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName = command.Args[0],
                        Arguments = string.Join(' ', command.Args.Skip(1)),
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    });
                    process?.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds);
                    result = process?.StandardOutput.ReadToEnd();
                    exitCode = process?.ExitCode ?? 1;
                    break;
                case WhichExe.CustomInteractive:
                    process = Process.Start(new ProcessStartInfo
                    {
                        FileName = command.Args[0],
                        Arguments = string.Join(' ', command.Args.Skip(1)),
                    });
                    process?.WaitForExit();
                    exitCode = process?.ExitCode ?? 1;
                    break;
            }

            return result;
        }

        #endregion
    }
}
