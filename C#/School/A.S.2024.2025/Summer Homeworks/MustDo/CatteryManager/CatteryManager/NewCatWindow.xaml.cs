using CatteryManagerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace CatteryManager
{
    /// <summary>
    /// Logica di interazione per NewCatWindow.xaml
    /// </summary>
    public partial class NewCatWindow : Window
    {
        private Cattery _cattery;
        
        public NewCatWindow(Cattery cattery)
        {
            InitializeComponent();
            _cattery = cattery;
            LoadBreeds();
        }

        private void LoadBreeds()
        {
            // Populate the breed combo box with all CatBreed enum values
            cmbBreed.ItemsSource = Enum.GetValues(typeof(CatBreeds)).Cast<CatBreeds>();
            cmbBreed.SelectedIndex = 0;
        }

        private void BtnAddCat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Please enter the cat's name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Arrival date (non-nullable, default to today if null)
                DateOnly arriveDate = DateOnly.FromDateTime(dpArrivalDate.SelectedDate ?? DateTime.Now);

                // Birth date (nullable)
                DateOnly? birthDate = dpBirthDate.SelectedDate.HasValue
                    ? DateOnly.FromDateTime(dpBirthDate.SelectedDate.Value)
                    : (DateOnly?)null;
                bool isMale = cmbGender.SelectedIndex == 0 ? true : false;
                string name = txtName.Text.Trim();

                Cat cat = new Cat(name, (CatBreeds)cmbBreed.SelectedItem,isMale,arriveDate,null,birthDate);


                _cattery.AddCat(cat); // Save the cat to persistence

                MessageBox.Show("Cat added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding cat: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

