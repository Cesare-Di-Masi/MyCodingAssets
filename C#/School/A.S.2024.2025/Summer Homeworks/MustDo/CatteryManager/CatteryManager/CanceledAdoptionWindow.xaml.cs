using CatteryManagerLib;
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

namespace CatteryManager
{
    /// <summary>
    /// Logica di interazione per CanceledAdoptionWindow.xaml
    /// </summary>
    public partial class CanceledAdoptionWindow : Window
    {
        private Cattery _cattery;
        public CanceledAdoptionWindow(Cattery cattery)
        {
            InitializeComponent();
            _cattery = cattery;
            LoadAdoptions();
        }

        private void LoadAdoptions()
        {
            var adoptions = _cattery.Adoptions;
            cmbAdoptions.ItemsSource = adoptions;

            if (adoptions.Count > 0)
                cmbAdoptions.SelectedIndex = 0;
        }

        private void BtnMarkFailed_Click(object sender, RoutedEventArgs e)
        {
            if (cmbAdoptions.SelectedItem is Adoption selectedAdoption)
            {
                var adoption = selectedAdoption;

                // Reset cat departure date
                _cattery.CancelAdoption(adoption);

                MessageBox.Show("Cat returned to cattery successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select an adoption.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
