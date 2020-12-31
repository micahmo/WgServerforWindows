using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using Bluegrams.Application;

namespace WireGuardServerForWindows
{
    public class MyUpdateChecker : WpfUpdateChecker
    {
        public MyUpdateChecker(string url, Window owner = null, string identifier = null) : base(url, owner, identifier)
        {
        }

        public override void ShowUpdateDownload(string file)
        {
            // Unzip the portable zip file
            string unzipDirectory = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
            ZipFile.ExtractToDirectory(file, unzipDirectory, overwriteFiles: true);

            string currentApplicationFilePath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            string currentApplicationFolderPath = Path.GetDirectoryName(currentApplicationFilePath);
            string backupPath = Path.Combine(Path.GetDirectoryName(currentApplicationFilePath), "OldVersion");

            // .NET Core app is technically running out of DLL, but we want to run EXE
            if (Path.GetExtension(currentApplicationFilePath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                currentApplicationFilePath = Path.Combine(currentApplicationFolderPath, $"{Path.GetFileNameWithoutExtension(currentApplicationFilePath)}.exe");
            }

            // If the backup directory already exists, delete it
            if (Directory.Exists(backupPath))
            {
                Directory.Delete(backupPath, recursive: true);
            }

            // Create the backup directory
            Directory.CreateDirectory(backupPath);

            string[] commands =
            {
                // Kill the current process
                $"taskkill /f /pid {Process.GetCurrentProcess().Id}",

                // Wait for the process to die before we can rename the exe
                $"timeout 1",

                // Move everything in the current directory to the OldVersion directory
                $"move /y \"{currentApplicationFolderPath}\\*\" \"{currentApplicationFolderPath}\\OldVersion\"",
                $"move \"{currentApplicationFolderPath}\\runtimes\" \"{currentApplicationFolderPath}\\OldVersion\"",

                // Move the extracted files to the running directory
                $"move \"{unzipDirectory}\\*\" \"{currentApplicationFolderPath}\"",
                $"move \"{unzipDirectory}\\runtimes\" \"{currentApplicationFolderPath}\"",

                // Launch the new exe. Use the explorer.exe trick to launch detached.
                $"explorer.exe \"{currentApplicationFilePath}\"",
            };

            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Verb = "runas", // For elevated privileges
                    FileName = "cmd.exe",
                    Arguments = "/C " + string.Join(" & ", commands)
                }
            }.Start();
        }
    }
}