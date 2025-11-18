using Application.Dto;
using Application.UseCases;
using System.Windows;

namespace Presentation
{
    /// <summary>
    /// Logica di interazione per CatManagerWindow.xaml
    /// </summary>
    public partial class CatManagerWindow : Window
    {
        public CatteryService CatteryService;

        public CatManagerWindow(CatteryService cattery)
        {
            InitializeComponent();
            CatteryService = cattery;
            dgCats.ItemsSource = CatteryService.ViewAllCats();
        }

        public void BtnBackToMain_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow(CatteryService);
            mainWindow.Show();
            this.Close();
        }

        public void BtnSeeAllCats_Click(object sender, RoutedEventArgs e)
        {
            dgCats.ItemsSource = CatteryService.ViewAllCats();
        }

        public void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            dgCats.ItemsSource = CatteryService.ViewAllCats();
        }

        public void BtnAddCat_Click(object sender, RoutedEventArgs e)
        {
            var addCatWindow = new AddCatWindow(CatteryService);
            addCatWindow.ShowDialog();
            dgCats.ItemsSource = CatteryService.ViewAllCats();
        }

        public void BtnEditCat_Click(object sender, RoutedEventArgs e)
        {
            if (dgCats.SelectedItem is CatDto selectedCat)
            {
                var editCatWindow = new AddCatWindow(CatteryService, selectedCat);
                editCatWindow.ShowDialog();
                dgCats.ItemsSource = CatteryService.ViewAllCats();
            }
            else
            {
                MessageBox.Show("Please select a cat to edit.", "No Cat Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void BtnDeleteCat_Click(object sender, RoutedEventArgs e)
        {
            if (dgCats.SelectedItem is CatDto selectedCat)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the cat '{selectedCat.Name}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        CatteryService.DeleteCat(selectedCat.Id ?? string.Empty);
                        MessageBox.Show("Cat deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        dgCats.ItemsSource = CatteryService.ViewAllCats();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a cat to delete.", "No Cat Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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