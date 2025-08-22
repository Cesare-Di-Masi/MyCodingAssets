using SpellArchiveLib;
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

namespace SpellArchive
{
    /// <summary>
    /// Logica di interazione per SchoolWindow.xaml
    /// </summary>
    public partial class SchoolWindow : Window
    {
        public SchoolWindow()
        {
            InitializeComponent();
            LstSchools.ItemsSource = Enum.GetValues(typeof(SpellsSchool));
        }

        private void BtnSearchSchool_Click(object sender, RoutedEventArgs e)
        {
            string query = TxtSearchSchool.Text.Trim().ToLower();
            foreach (var item in LstSchools.Items)
            {
                if (item.ToString().ToLower().Contains(query))
                {
                    LstSchools.SelectedItem = item;
                    break;
                }
            }
        }

        private void BtnAddSchool_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Le scuole sono enum, quindi non puoi aggiungerne dinamicamente a runtime.\n" +
                "Se vuoi farlo, devi trasformare SpellsSchool in una classe persistente.", "Nota");
        }

    }
}
