using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class UnhandledErrorWindowModel : ObservableObject
    {
        public string Title
        {
            get => _title;
            set => Set(nameof(Title), ref _title, value);
        }
        private string _title;

        public string Text
        {
            get => _text;
            set => Set(nameof(Text), ref _text, value);
        }
        private string _text;

        public Exception Exception
        {
            get => _exception;
            set => Set(nameof(Exception), ref _exception, value);
        }
        private Exception _exception;

        public string SecondaryButtonText
        {
            get => _secondaryButtonText;
            set => Set(nameof(SecondaryButtonText), ref _secondaryButtonText, value);
        }
        private string _secondaryButtonText = Resources.CopyDetails;

        public Action SecondaryButtonAction
        {
            get => _secondaryButtonAction ?? CopyExceptionToClipboard;
            set => _secondaryButtonAction = value;

        }
        private Action _secondaryButtonAction;

        public ICommand CopyErrorCommand => _copyErrorCommand ??= new RelayCommand(() => SecondaryButtonAction?.Invoke());
        private RelayCommand _copyErrorCommand;

        private void CopyExceptionToClipboard()
        {
            var exception = Exception;
            StringBuilder exceptionText = new StringBuilder();
            while (exception is { })
            {
                exceptionText.Append(exception);
                exceptionText.Append(Environment.NewLine);
                exceptionText.Append(Environment.NewLine);
                exception = exception.InnerException;
            }

            // This can help to alleviate issues opening the clipboard like CLIPBRD_E_CANT_OPEN
            // See: https://stackoverflow.com/a/69081 
            foreach (var _ in Enumerable.Range(0, 10))
            {
                try
                {
                    Clipboard.SetText(exceptionText.ToString());
                    break;
                }
                catch { }
                Thread.Sleep(10);
            }
        }
    }
}
