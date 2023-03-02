using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Xceed.Wpf.AvalonDock.Controls;

namespace WgServerforWindows.Models
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

            SubCommands.CollectionChanged += (_, __) =>
            {
                RaisePropertyChanged(nameof(HasSubCommands));
            };

            Refresh();
        }

        #endregion

        #region Overrides

        /// <inheritdoc/>
        public override void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName == nameof(CanResolve))
            {
                Commands.ResolveCommand.RaiseCanExecuteChanged();
            }

            if (propertyName == nameof(CanConfigure))
            {
                
                Commands.ConfigureCommand.RaiseCanExecuteChanged();
            }

            base.RaisePropertyChanged(propertyName);
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

        public virtual string SuccessMessage
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

        public virtual BooleanTimeCachedProperty HasIcon { get; } = new BooleanTimeCachedProperty(TimeSpan.Zero, () => true);

        public bool CanResolve => CanResolveFunc?.Invoke() ?? true;

        public Func<bool> CanResolveFunc { get; set; }

        public bool CanConfigure => CanConfigureFunc?.Invoke() ?? true;

        public Func<bool> CanConfigureFunc { get; set; }

        public ObservableCollection<PrerequisiteItem> Children { get; } = new ObservableCollection<PrerequisiteItem>();

        public bool HasChildren => Children.Any();

        public ObservableCollection<PrerequisiteItem> SubCommands { get; } = new ObservableCollection<PrerequisiteItem>();

        public bool HasSubCommands => SubCommands.Any();

        public virtual string Category { get; }

        public IEnumerable<IGrouping<string, PrerequisiteItem>> ChildrenByCategory => Children.GroupBy(c => c.Category);

        public int SelectedChildIndex { get; set; }

        public Control Control => Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault()?.FindVisualChildren<Control>().FirstOrDefault(b => b.DataContext == this);

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

    public class PrerequisiteItemCommands : ObservableObject
    {
        public PrerequisiteItemCommands(PrerequisiteItem prerequisiteItem)
        {
            PrerequisiteItem = prerequisiteItem;
        }

        private PrerequisiteItem PrerequisiteItem { get; }

        #region ICommands

        // The canExecute parameter is only needed for xctk:SplitButton, which uses ICommand.CanExecute to determine enabled status (instead of IsEnabled) when a Command is bound.
        // https://github.com/xceedsoftware/wpftoolkit/issues/1466
        public RelayCommand ResolveCommand => _resolveCommand ??= new RelayCommand(PrerequisiteItem.Resolve, PrerequisiteItem.HasSubCommands ? PrerequisiteItem.CanResolveFunc : null);
        private RelayCommand _resolveCommand;

        // The canExecute parameter is only needed for xctk:SplitButton, which uses ICommand.CanExecute to determine enabled status (instead of IsEnabled) when a Command is bound.
        // https://github.com/xceedsoftware/wpftoolkit/issues/1466
        public RelayCommand ConfigureCommand => _configureCommand ??= new RelayCommand(PrerequisiteItem.Configure, PrerequisiteItem.HasSubCommands ? PrerequisiteItem.CanConfigureFunc : null);
        private RelayCommand _configureCommand;

        #endregion
    }
}
