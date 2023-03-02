using System;
using System.IO;
using GalaSoft.MvvmLight;
using Jot;
using Jot.Storage;

namespace WgServerforWindows.Models
{
    /// <summary>
    /// Defines system-wide, application-wide settings which will be persisted across sessions
    /// </summary>
    internal class GlobalAppSettings : ObservableObject
    {
        #region Singleton member

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static GlobalAppSettings Instance { get; } = new GlobalAppSettings();

        #endregion

        #region Private constructor

        /// <summary>
        /// Constructor
        /// </summary>
        private GlobalAppSettings()
        {
            // Set up AppSettings tracking
            Tracker.Configure<GlobalAppSettings>()
                .Property(a => a.BootTaskDelay)
                .Track(this);
        }

        #endregion

        #region Public methods

        public void Save()
        {
            Tracker.Persist(this);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Boot task delay time
        /// </summary>
        public TimeSpan BootTaskDelay
        {
            get => _bootTaskDelay;
            set => Set(nameof(BootTaskDelay), ref _bootTaskDelay, value);
        }
        private TimeSpan _bootTaskDelay;

        /// <summary>
        /// The public tracker instance located in Public\Documents. Can be used to track things other than the <see cref="Instance"/>.
        /// </summary>
        public Tracker Tracker { get; } = new Tracker(new JsonFileStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "WS4W")));

        #endregion
    }
}
