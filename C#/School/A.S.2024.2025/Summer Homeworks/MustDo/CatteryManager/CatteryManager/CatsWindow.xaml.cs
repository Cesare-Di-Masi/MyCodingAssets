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
    /// Logica di interazione per CatsWindow.xaml
    /// </summary>
    public partial class CatsWindow : Window
    {
        private Cattery _cattery;
        public CatsWindow(Cattery cattery)
        {
            InitializeComponent();
            _cattery = cattery;
            LoadCats();
        }

        private void LoadCats()
        {
            var cats = _cattery.Cats; // Retrieves all cats from persistence
            lstCats.ItemsSource = cats;
        }

        private void LstCats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstCats.SelectedItem is Cat selectedCat)
            {
                txtName.Text = selectedCat.Name;
                txtBreed.Text = selectedCat.Breed.ToString();
                txtGender.Text = selectedCat.IsMale.ToString();
                txtArrival.Text = selectedCat.ArriveDate.ToShortDateString();
                txtDeparture.Text = selectedCat.ExitDate?.ToShortDateString() ?? "Still in cattery";
                txtBirth.Text = selectedCat.BirthDate?.ToShortDateString() ?? "Unknown";
            }
        }
    }
}
