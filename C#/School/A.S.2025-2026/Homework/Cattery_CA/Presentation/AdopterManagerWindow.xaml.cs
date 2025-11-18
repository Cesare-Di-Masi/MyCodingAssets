using Application.UseCases;
using System.Windows;

namespace Presentation
{
    /// <summary>
    /// Logica di interazione per AdopterManagerWindow.xaml
    /// </summary>
    public partial class AdopterManagerWindow : Window
    {
        public CatteryService CatteryService;

        public AdopterManagerWindow(CatteryService cattery)
        {
            InitializeComponent();
            CatteryService = cattery;
        }

        private void BtnMenuCat_Add_Click(object sender, RoutedEventArgs e)
        {
            var addCatWindow = new AddCatWindow(CatteryService);
            addCatWindow.Show();
            this.Close();
        }

        private void BtnMenuCat_Manage_Click(object sender, RoutedEventArgs e)
        {
            var catManageWindow = new CatManagerWindow(CatteryService);
            catManageWindow.Show();
            this.Close();
        }

        private void BtnMenuAdopter_Manage_Click(object sender, RoutedEventArgs e)
        {
            var adopterManageWindow = new AdopterManagerWindow(CatteryService);
            adopterManageWindow.Show();
            this.Close();
        }

        private void BtnMenuAdoption_Manage_Click(object sender, RoutedEventArgs e)
        {
            var adoptionManageWindow = new AdoptionManagerWindow(CatteryService);
            adoptionManageWindow.Show();
            this.Close();
        }

        private void BtnMenuAdoption_Add_Click(object sender, RoutedEventArgs e)
        {
            var addAdoptionWindow = new AddAdoptionWindow(CatteryService);
            addAdoptionWindow.Show();
            this.Close();
        }

        private void BtnMenuAdopter_Add_Click(object sender, RoutedEventArgs e)
        {
            var addAdopterWindow = new AddAdopterWindows(CatteryService);
            addAdopterWindow.Show();
            this.Close();
        }
    }
}