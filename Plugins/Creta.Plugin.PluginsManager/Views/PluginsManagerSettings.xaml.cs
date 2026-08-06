using Creta.Plugin.PluginsManager.ViewModels;

namespace Creta.Plugin.PluginsManager.Views
{
    /// <summary>
    /// Interaction logic for PluginsManagerSettings.xaml
    /// </summary>
    public partial class PluginsManagerSettings
    {
        internal PluginsManagerSettings(SettingsViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}
