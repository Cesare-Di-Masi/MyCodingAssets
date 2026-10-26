using SpellArchiveLib;
using System.Windows;

namespace SpellArchive
{
    /// <summary>
    /// Logica di interazione per SpellBySchoolWindow.xaml
    /// </summary>
    public partial class SpellBySchoolWindow : Window
    {
        private Archive _archive;

        public SpellBySchoolWindow(Archive archive)
        {
            InitializeComponent();
            _archive = archive;
            CmbSchools.ItemsSource = Enum.GetValues(typeof(SpellsSchool)).Cast<SpellsSchool>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Carica subito tutti gli incantesimi
            LstSpells.ItemsSource = _archive.SpellArchive;
        }

        private void BtnLoadSpells_Click(object sender, RoutedEventArgs e)
        {
            if (CmbSchools.SelectedItem is SpellsSchool school)
            {
                var spells = _archive.FindSpellsBySchool(school);
                LstSpells.ItemsSource = spells;
            }
            else
            {
                // Nessuna scuola selezionata → mostra tutti
                LstSpells.ItemsSource = _archive.SpellArchive;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            var window = new MainWindow(_archive);
            window.Show();
            this.Close();
        }
    }
}