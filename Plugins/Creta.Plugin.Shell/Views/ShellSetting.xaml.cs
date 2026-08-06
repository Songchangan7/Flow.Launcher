using System.Windows.Controls;
using Creta.Plugin.Shell.ViewModels;

namespace Creta.Plugin.Shell.Views
{
    public partial class CMDSetting : UserControl
    {
        public CMDSetting(Settings settings)
        {
            var viewModel = new ShellSettingViewModel(settings);
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
