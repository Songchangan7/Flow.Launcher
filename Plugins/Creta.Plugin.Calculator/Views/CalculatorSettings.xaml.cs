using System.Windows.Controls;
using Creta.Plugin.Calculator.ViewModels;

namespace Creta.Plugin.Calculator.Views;

public partial class CalculatorSettings : UserControl
{
    private readonly SettingsViewModel _viewModel;

    public CalculatorSettings(Settings settings)
    {
        _viewModel = new SettingsViewModel(settings);
        DataContext = _viewModel;
        InitializeComponent();
    }
}
