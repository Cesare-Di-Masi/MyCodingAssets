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
    /// Logica di interazione per AdoptionWindow.xaml
    /// </summary>
    public partial class AdoptionWindow : Window
    {
        Cattery _cattery;
        public AdoptionWindow(Cattery cattery)
        {
            InitializeComponent();
            LoadAdoptions();
        }

        private void LoadAdoptions()
        {
            var adoptions = _cattery.Adoptions; // Retrieves all adoptions from persistence
            dgAdoptions.ItemsSource = adoptions;
        }
    }
}
