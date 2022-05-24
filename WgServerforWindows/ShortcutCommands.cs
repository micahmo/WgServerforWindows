using System.Windows.Input;

namespace WgServerforWindows
{
    public static class ShortcutCommands
    {
        #region Commands

        static ShortcutCommands()
        {
            AboutBoxCommand.InputGestures.Add(new KeyGesture(Key.F1));
            RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
        }

        public static RoutedCommand AboutBoxCommand { get; } = new RoutedCommand();

        public static RoutedCommand RefreshCommand { get; } = new RoutedCommand();

        #endregion
    }
}