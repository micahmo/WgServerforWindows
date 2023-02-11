using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using GalaSoft.MvvmLight;
using Jot;
using Jot.Storage;

namespace WgServerforWindows.Models
{
    /// <summary>
    /// Defines application-wide settings which will be persisted across sessions
    /// </summary>
    internal class AppSettings : ObservableObject
    {
        #region Singleton member

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static AppSettings Instance { get; } = new AppSettings();

        #endregion

        #region Private constructor

        /// <summary>
        /// Constructor
        /// </summary>
        private AppSettings()
        {
            // Set up Window tracking
            Tracker.Configure<Window>()
                .Id(w => w.Name, new Size(SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight))
                .Properties(w => new { w.Top, w.Width, w.Height, w.Left, w.WindowState })
                .PersistOn(nameof(Window.Closing))
                .StopTrackingOn(nameof(Window.Closing))
                .WhenPersistingProperty((w, p) => p.Cancel = p.Property == nameof(w.WindowState) && w.WindowState == WindowState.Minimized);
        }

        #endregion

        #region Public methods

        public void Load()
        {
            // Set up AppSettings tracking
            Tracker.Configure<AppSettings>()
                .Property(a => a.CustomServerConfigDirectory)
                .Property(a => a.CustomClientConfigDirectory)
                .Property(a => a.ClientConfigurationExpansionStates)
                .Track(this);
        }

        public void Save()
        {
            Tracker.Persist(this);
        }

        #endregion

        #region Public properties

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

        /// <summary>
        /// Tracks whether each client configuration is expanded in the UI or not
        /// </summary>
        public Dictionary<string, bool> ClientConfigurationExpansionStates = new Dictionary<string, bool>();

        /// <summary>
        /// The public tracker instance. Can be used to track things other than the <see cref="Instance"/>.
        /// </summary>
        public Tracker Tracker { get; } = new Tracker(new JsonFileStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WS4W")));

        #endregion
    }
}
