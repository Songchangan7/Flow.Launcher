using System;
using System.Windows;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.DependencyInjection;
using Creta.Helper;
using Creta.Infrastructure.UserSettings;
using Creta.ViewModel;

namespace Creta.Resources.Pages
{
    public partial class WelcomePage5
    {
        public Settings Settings { get; } = Ioc.Default.GetRequiredService<Settings>();
        private readonly WelcomeViewModel _viewModel = Ioc.Default.GetRequiredService<WelcomeViewModel>();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Sometimes the navigation is not triggered by button click,
            // so we need to reset the page number
            _viewModel.PageNum = 5;

            if (!IsInitialized)
            {
                InitializeComponent();
            }
            base.OnNavigatedTo(e);
        }

        private void OnAutoStartupChecked(object sender, RoutedEventArgs e)
        {
            ChangeAutoStartup(true);
        }

        private void OnAutoStartupUncheck(object sender, RoutedEventArgs e)
        {
            ChangeAutoStartup(false);
        }

        private void ChangeAutoStartup(bool value)
        {
            Settings.StartCretaOnSystemStartup = value;
            try
            {
                if (value)
                {
                    if (Settings.UseLogonTaskForStartup)
                    {
                        AutoStartup.ChangeToViaLogonTask();
                    }
                    else
                    {
                        AutoStartup.ChangeToViaRegistry();
                    }
                }
                else
                {
                    AutoStartup.DisableViaLogonTaskAndRegistry();
                }
            }
            catch (Exception e)
            {
                App.API.ShowMsgError(Localize.setAutoStartFailed(), e.Message);
            }
        }

        private void OnHideOnStartupChecked(object sender, RoutedEventArgs e)
        {
            Settings.HideOnStartup = true;
        }

        private void OnHideOnStartupUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.HideOnStartup = false;
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.Close();
        }
    }
}
