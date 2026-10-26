using SpellArchiveLib;
using System.Windows;

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