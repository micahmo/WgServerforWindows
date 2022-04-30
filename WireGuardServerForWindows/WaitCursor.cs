using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace WireGuardServerForWindows
{
    // https://stackoverflow.com/a/3481274/4206279
    public class WaitCursor : IDisposable
    {
        private readonly Cursor _previousCursor;
        private readonly DispatcherPriority _dispatcherPriority;

        public WaitCursor(DispatcherPriority dispatcherPriority = DispatcherPriority.Normal, bool restoreCursorToNull = false)
        {
            _previousCursor = restoreCursorToNull ? null : Mouse.OverrideCursor;
            _dispatcherPriority = dispatcherPriority;

            SetOverrideCursor(Cursors.Wait);
        }

        public WaitCursor(DispatcherPriority dispatcherPriority = DispatcherPriority.Normal, Cursor restoreCursor = null)
        {
            _previousCursor = restoreCursor ?? Mouse.OverrideCursor;
            _dispatcherPriority = dispatcherPriority;

            SetOverrideCursor(Cursors.Wait);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SetOverrideCursor(_previousCursor);
            }, _dispatcherPriority);
        }

        #endregion

        /// <summary>
        /// Globally set the <see cref="Mouse.OverrideCursor"/>.
        /// </summary>
        /// <param name="cursor"></param>
        public static void SetOverrideCursor(Cursor cursor)
        {
            if (!IgnoreOverrideCursor)
            {
                Mouse.OverrideCursor = cursor;
            }
        }

        /// <summary>
        /// Whether or not to allow <see cref="SetOverrideCursor(Cursor)"/> to take effect.
        /// </summary>
        public static bool IgnoreOverrideCursor { get; set; }
    }
}