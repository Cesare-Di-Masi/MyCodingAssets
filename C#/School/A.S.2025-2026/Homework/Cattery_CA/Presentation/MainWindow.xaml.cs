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
        public CatteryService CatteryService = new CatteryService(new JsonCatRepository(), new JsonAdopterRepository(), new JsonAdoptionRepository());

        public MainWindow()
        {
            InitializeComponent();
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
    }
}