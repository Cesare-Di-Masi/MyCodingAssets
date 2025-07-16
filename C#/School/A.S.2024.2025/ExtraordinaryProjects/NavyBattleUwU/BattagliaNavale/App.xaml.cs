using BattagliaNavale.Services;
using BattagliaNavale.Views;

namespace BattagliaNavale
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new Home());
        }

        protected override void OnResume()
        {
            base.OnResume();
            AudioPlayerService.Instance.Play();
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            AudioPlayerService.Instance.Pause();
        }
    }
}
