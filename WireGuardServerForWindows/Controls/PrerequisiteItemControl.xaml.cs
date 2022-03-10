using System.Windows;
using System.Windows.Controls;

namespace WireGuardServerForWindows.Controls
{
    /// <summary>
    /// Interaction logic for PrerequisiteItemControl.xaml
    /// </summary>
    public partial class PrerequisiteItemControl : UserControl
    {
        public static readonly DependencyProperty IsChildProperty = DependencyProperty.Register(nameof(IsChild), typeof(bool), typeof(PrerequisiteItemControl));

        public PrerequisiteItemControl()
        {
            InitializeComponent();
        }

        public bool IsChild
        {
            get => (bool)GetValue(IsChildProperty);
            set => SetValue(IsChildProperty, value);
        }
    }
}
