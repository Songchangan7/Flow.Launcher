using System.Windows.Navigation;
using CommunityToolkit.Mvvm.DependencyInjection;
using Creta.Infrastructure.UserSettings;
using Creta.ViewModel;

namespace Creta.Resources.Pages
{
    public partial class WelcomePage3
    {
        public Settings Settings { get; } = Ioc.Default.GetRequiredService<Settings>();
        private readonly WelcomeViewModel _viewModel = Ioc.Default.GetRequiredService<WelcomeViewModel>();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Sometimes the navigation is not triggered by button click,
            // so we need to reset the page number
            _viewModel.PageNum = 3;

            if (!IsInitialized)
            {
                InitializeComponent();
            }
            base.OnNavigatedTo(e);
        }
    }
}
