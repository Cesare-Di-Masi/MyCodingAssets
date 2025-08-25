using CatteryManagerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
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
    /// Logica di interazione per NewAdoptionWindow.xaml
    /// </summary>
    public partial class NewAdoptionWindow : Window
    {
        private Cattery _cattery;

        public NewAdoptionWindow(Cattery cattery)
        {
            InitializeComponent();
            LoadAvailableCats();
        }

        private void LoadAvailableCats()
        {
            // Load only cats currently in the cattery (DepartureDate == null)
            var cats = _cattery.Cats.ToList();
            cmbCats.ItemsSource = cats;

            if (cats.Count > 0)
                cmbCats.SelectedIndex = 0;
        }

        private void BtnRegisterAdoption_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCats.SelectedItem == null)
                {
                    MessageBox.Show("Please select a cat.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtAdopterName.Text) ||
                    string.IsNullOrWhiteSpace(txtAdopterSurname.Text) ||
                    string.IsNullOrWhiteSpace(txtAddress.Text) ||
                    string.IsNullOrWhiteSpace(txtPhone.Text))
                {
                    MessageBox.Show("Please fill in all adopter details.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedCat = (Cat)cmbCats.SelectedItem;


                string Name = txtAdopterName.Text.Trim(),
                Surname = txtAdopterSurname.Text.Trim(),
                Mail = txtAddress.Text.Trim(),
                Phone = txtPhone.Text.Trim();


                Adopter adopter = new Adopter(Name, Surname, Mail, Phone);

                Adoption adoption = new Adoption(selectedCat, adopter);

                _cattery.RegisterAdoption(adoption); // Save the adoption to persistence

                MessageBox.Show("Adoption registered successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error registering adoption: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
