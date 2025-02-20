using System;
using System.IO;

namespace WgServerforWindows.Models
{
    /// <summary>
    /// Allows creating a temporary file that is automatically deleted when disposed
    /// </summary>
    public class TemporaryFile : IDisposable
    {
        public TemporaryFile(string originalFilePath, string newFilePath)
        {
            OriginalFilePath = originalFilePath;
            NewFilePath = newFilePath;

            if (TemporaryFileIsNeeded)
            {
                File.Copy(OriginalFilePath, NewFilePath, overwrite: true);
            }
        }

        public string OriginalFilePath { get; init; }
        
        public string NewFilePath { get; init; }

        public bool TemporaryFileIsNeeded => NewFilePath != OriginalFilePath;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (TemporaryFileIsNeeded && File.Exists(NewFilePath))
            {
                File.Delete(NewFilePath);
            }
        }
    }
}
