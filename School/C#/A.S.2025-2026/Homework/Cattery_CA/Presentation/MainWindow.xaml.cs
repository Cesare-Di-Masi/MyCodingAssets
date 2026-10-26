using Application.UseCases;
using Infrastructure.Repositories;
using System.Windows;

namespace Presentation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CatteryService CatteryService;
        private JsonAdopterRepository JsonAdopterRepository = new JsonAdopterRepository();
        private JsonCatRepository JsonCatRepository = new JsonCatRepository();
        private JsonAdoptionRepository JsonAdoptionRepository = new JsonAdoptionRepository();

        public MainWindow()
        {
            InitializeComponent();
            CatteryService = new CatteryService(JsonCatRepository, JsonAdopterRepository, JsonAdoptionRepository);
            Update();
        }

        public MainWindow(CatteryService cattery)
        {
            InitializeComponent();
            CatteryService = cattery;
            Update();
        }

        private void Update()
        {
            TxTBlockCatCount.Text = CatteryService.GetTotalCatsCount().ToString();
            TxTBlockFemalesCatCount.Text = CatteryService.GetFemaleCatsCount().ToString();
            TxTBlockMalesCatCount.Text = CatteryService.GetMaleCatsCount().ToString();
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