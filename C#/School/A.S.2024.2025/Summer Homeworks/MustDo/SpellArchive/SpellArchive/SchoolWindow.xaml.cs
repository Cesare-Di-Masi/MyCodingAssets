using System.Windows;
using SpellArchiveLib;
namespace SpellArchive
{
    public partial class SchoolWindow : Window
    {
        public SchoolWindow()
        {
            InitializeComponent();
            LstSchools.ItemsSource = System.Enum.GetValues(typeof(SpellArchiveLib.SpellsSchool));
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