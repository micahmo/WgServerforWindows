using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Command;

namespace WireGuardServerForWindows.Models
{
    public class ClientConfigurationList
    {
        public ObservableCollection<ClientConfiguration> List { get; } = new ObservableCollection<ClientConfiguration>();

        public ICommand AddClientConfigurationCommand => _addClientConfigurationCommand ??= new RelayCommand(() =>
        {
            using (new WaitCursor(dispatcherPriority: DispatcherPriority.Render, restoreCursorToNull: true))
            {
                List.Add(new ClientConfiguration(this));
            }
        });
        private RelayCommand _addClientConfigurationCommand;
    }
}
