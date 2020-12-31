using System.Collections.ObjectModel;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace WireGuardServerForWindows.Models
{
    public class SelectionWindowModel<T> : ObservableObject
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

        public ObservableCollection<SelectionItem<T>> Items { get; } = new ObservableCollection<SelectionItem<T>>();

        public SelectionItem<T> SelectedItem
        {
            get => _selectedItem;
            set
            {
                Set(nameof(SelectedItem), ref _selectedItem, value);
                RaisePropertyChanged(nameof(CanSelect));
            }
        }
        private SelectionItem<T> _selectedItem;

        public bool? DialogResult { get; private set; }

        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(() =>
        {
            DialogResult = false;
        });
        private RelayCommand _cancelCommand;

        public ICommand SelectCommand => _selectCommand ??= new RelayCommand(() =>
        {
            DialogResult = true;
        });
        private RelayCommand _selectCommand;

        public bool CanSelect => SelectedItem is { };
    }

    public class SelectionItem : ObservableObject
    {
        public string DisplayText
        {
            get => _displayText;
            set => Set(nameof(DisplayText), ref _displayText, value);
        }
        private string _displayText;

        public string Description
        {
            get => _description;
            set => Set(nameof(Description), ref _description, value);
        }
        private string _description;
    }

    public class SelectionItem<T> : SelectionItem
    {
        public T BackingObject
        {
            get => _backingObject;
            set => Set(nameof(BackingObject), ref _backingObject, value);
        }
        private T _backingObject;
    }
}
