using System;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace WireGuardServerForWindows.Models
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

        public ICommand CopyErrorCommand => _copyErrorCommand ??= new RelayCommand(() =>
        {
            Clipboard.SetText(Exception.ToString()); // ToString contains the most information, like stack trace (more than Message)
        });
        private RelayCommand _copyErrorCommand;
    }
}
