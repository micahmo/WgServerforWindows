using System.Windows.Input;

namespace WireGuardServerForWindows
{
    public static class ShortcutCommands
    {
        #region Commands

        static ShortcutCommands()
        {
            AboutBoxCommand.InputGestures.Add(new KeyGesture(Key.F1));
        }

        public static RoutedCommand AboutBoxCommand { get; } = new RoutedCommand();

        #endregion
    }
}