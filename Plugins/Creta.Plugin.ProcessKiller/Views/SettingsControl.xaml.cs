using System.Windows.Controls;
using Creta.Plugin.ProcessKiller.ViewModels;

namespace Creta.Plugin.ProcessKiller.Views;

public partial class SettingsControl : UserControl
{
    public SettingsControl(SettingsViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}
