using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WgServerforWindows.Models
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

        public string ValidationError
        {
            get => _validationError;
            set => Set(nameof(ValidationError), ref _validationError, value);
        }
        private string _validationError;

        public double MinWidth
        {
            get => _minWidth;
            set => Set(nameof(MinWidth), ref _minWidth, value);
        }
        private double _minWidth;

        public ObservableCollection<SelectionItem<T>> Items { get; } = new ObservableCollection<SelectionItem<T>>();

        public SelectionItem<T> SelectedItem
        {
            get => _selectedItem;
            set
            {
                Set(nameof(SelectedItem), ref _selectedItem, value);
                RaisePropertyChanged(nameof(CanSelect));

                SelectedItem.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(SelectedItem.BackingObject))
                    {
                        RaisePropertyChanged(nameof(CanSelect));
                    }
                };
            }
        }
        private SelectionItem<T> _selectedItem;

        public bool IsList { get; set; } = true;

        public bool IsTimeSpan { get; set; }

        public bool IsString { get; set; }

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

        public bool CanSelect => CanSelectFunc?.Invoke() ?? SelectedItem is { };

        public Func<bool> CanSelectFunc { get; set; }
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
