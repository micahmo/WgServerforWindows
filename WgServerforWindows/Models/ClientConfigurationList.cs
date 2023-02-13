using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using WgServerforWindows.Properties;

namespace WgServerforWindows.Models
{
    public class ClientConfigurationList : ObservableObject
    {
        public ClientConfigurationList()
        {
            List.CollectionChanged += (_, __) =>
            {
                RaisePropertyChanged(nameof(CountString));
            };
        }
        
        public ObservableCollection<ClientConfiguration> List { get; } = new ObservableCollection<ClientConfiguration>();

        public string CountString => List.Any(c => !c.IsVisible) 
            ? string.Format(Resources.FilteredClientCount, List.Count(c => c.IsVisible), List.Count) 
            : string.Format(Resources.ClientCount, List.Count);

        public ICommand AddClientConfigurationCommand => _addClientConfigurationCommand ??= new RelayCommand(() =>
        {
            using (new WaitCursor(dispatcherPriority: DispatcherPriority.Render, restoreCursorToNull: true))
            {
                List.Add(new ClientConfiguration(this));
            }
        });
        private RelayCommand _addClientConfigurationCommand;

        public ICommand ExpandAllConfigurationsCommand => _expandAllConfigurationsCommand ??= new RelayCommand(() =>
        {
            List.ToList().ForEach(c => c.IsExpanded = true);
        });
        private RelayCommand _expandAllConfigurationsCommand;

        public ICommand CollapseAllConfigurationsCommand => _collapseAllConfigurationsCommand ??= new RelayCommand(() =>
        {
            List.ToList().ForEach(c => c.IsExpanded = false);
        });
        private RelayCommand _collapseAllConfigurationsCommand;
    }
}
