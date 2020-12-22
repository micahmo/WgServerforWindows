using System.Collections.Generic;
using GalaSoft.MvvmLight;

namespace WireGuardServerForWindows.Models
{
    public class MainWindowModel : ObservableObject
    {
        public MainWindowModel()
        {
            
        }

        public List<PrerequisiteItem> PrerequisiteItems { get; set; } = new List<PrerequisiteItem>();
    }
}
