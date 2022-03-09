using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace WireGuardServerForWindows.Models
{
    public abstract class PrerequisiteItem : ObservableObject
    {
        #region Constructor

        protected PrerequisiteItem(string title, string successMessage, string errorMessage, string resolveText, string configureText)
        {
            Title = title;
            SuccessMessage = successMessage;
            ErrorMessage = errorMessage;
            ResolveText = resolveText;
            ConfigureText = configureText;
            Commands = new PrerequisiteItemCommands(this);

            Children.CollectionChanged += (_, __) =>
            {
                RaisePropertyChanged(nameof(HasChildren));
            };

            Refresh();
        }

        #endregion

        #region Public properties

        public PrerequisiteItemCommands Commands { get; }

        public string Title
        {
            get => _title;
            set => Set(nameof(Title), ref _title, value);
        }
        private string _title;

        public virtual BooleanTimeCachedProperty Fulfilled { get; } = new BooleanTimeCachedProperty(TimeSpan.Zero, () => true);

        public string SuccessMessage
        {
            get => _successMessage;
            set => Set(nameof(SuccessMessage), ref _successMessage, value);
        }
        private string _successMessage;

        public string ErrorMessage
        {
            get => _errorMessage;
            set => Set(nameof(ErrorMessage), ref _errorMessage, value);
        }
        private string _errorMessage;

        public string ResolveText
        {
        get => _resolveText;
            set => Set(nameof(ResolveText), ref _resolveText, value);
        }
        private string _resolveText;

        public string ConfigureText
        {
            get => _configureText;
            set => Set(nameof(ConfigureText), ref _configureText, value);
        }
        private string _configureText;

        public virtual BooleanTimeCachedProperty IsInformational { get; } = new BooleanTimeCachedProperty(TimeSpan.Zero, () => false);

        public bool CanResolve => CanResolveFunc?.Invoke() ?? true;

        public Func<bool> CanResolveFunc { get; set; }

        public bool CanConfigure => CanConfigureFunc?.Invoke() ?? true;

        public Func<bool> CanConfigureFunc { get; set; }

        public ObservableCollection<PrerequisiteItem> Children { get; } = new ObservableCollection<PrerequisiteItem>();

        public virtual string Category { get; }

        public bool HasChildren => Children.Any();

        public IEnumerable<IGrouping<string, PrerequisiteItem>> ChildrenByCategory => Children.GroupBy(c => c.Category);

        public int SelectedChildIndex { get; set; }

        #endregion

        #region Public methods

        public virtual void Resolve() { }

        public virtual void Configure() { }

        public void Refresh()
        {
            RaisePropertyChanged(nameof(Fulfilled));
            RaisePropertyChanged(nameof(IsInformational));
        }

        public async Task WaitForFulfilled()
        {
            await WaitForFulfilled(true);
        }

        public async Task WaitForFulfilled(bool value)
        {
            while (Fulfilled != value)
            {
                await Task.Delay((int) TimeSpan.FromSeconds(1).TotalMilliseconds);
            }

            Refresh();
        }

        public virtual void Update() { }

        #endregion
    }

    public class PrerequisiteItemCommands
    {
        public PrerequisiteItemCommands(PrerequisiteItem prerequisiteItem)
        {
            PrerequisiteItem = prerequisiteItem;
        }

        private PrerequisiteItem PrerequisiteItem { get; }

        #region ICommands

        public ICommand ResolveCommand => _resolveCommand ??= new RelayCommand(PrerequisiteItem.Resolve);
        private RelayCommand _resolveCommand;

        public ICommand ConfigureCommand => _configureCommand ??= new RelayCommand(PrerequisiteItem.Configure);
        private RelayCommand _configureCommand;

        #endregion

        #region Command implementations

        #endregion
    }
}
