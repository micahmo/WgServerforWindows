using System;
using System.IO;
using GalaSoft.MvvmLight;
using Jot;
using Jot.Storage;

namespace WireGuardServerForWindows.Models
{
    /// <summary>
    /// Defines application-wide settings which will be persisted across sessions
    /// </summary>
    internal class AppSettings : ObservableObject
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static AppSettings Instance { get; } = new AppSettings();

        public void Load()
        {
            Tracker.Configure<AppSettings>()
                .Property(a => a.CustomServerConfigDirectory)
                .Property(a => a.CustomClientConfigDirectory)
                .Track(this);
        }

        public void Save()
        {
            Tracker.Persist(this);
        }

        /// <summary>
        /// The parent directory of the server configuration files
        /// </summary>
        public string CustomServerConfigDirectory
        {
            get => _customServerConfigDirectory;
            set => Set(nameof(CustomServerConfigDirectory), ref _customServerConfigDirectory, value);
        }
        private string _customServerConfigDirectory;

        /// <summary>
        /// The parent directory of the client configuration files
        /// </summary>
        public string CustomClientConfigDirectory
        {
            get => _customClientConfigDirectory;
            set => Set(nameof(CustomClientConfigDirectory), ref _customClientConfigDirectory, value);
        }
        private string _customClientConfigDirectory;

        #region Private fields

        private static readonly Tracker Tracker = new Tracker(new JsonFileStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W")));

        #endregion
    }
}
