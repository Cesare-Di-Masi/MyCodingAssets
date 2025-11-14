using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Application.Dto;
using Application.UseCases;

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
    }
}