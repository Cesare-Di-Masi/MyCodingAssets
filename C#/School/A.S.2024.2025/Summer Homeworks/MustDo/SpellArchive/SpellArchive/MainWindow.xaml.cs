using System.Windows;
using SpellArchiveLib;

namespace SpellArchive
{
    public partial class MainWindow : Window
    {
        private Archive _archive;

        public MainWindow(Archive archive)
        {
            InitializeComponent();
            _archive = archive;
            UpdateRareCount();
        }

        private void UpdateRareCount()
        {
            // "Raro" devi deciderlo tu, qui metto esempio con Accessibility.Rare
            int rareCount = _archive.FindSpellsByAccessibility(Accessibility.Rare).Count;
            TxtRareCount.Text = rareCount.ToString();
        }

        private void BtnFindCRA_Click(object sender, RoutedEventArgs e)
        {
            string cra = TxtCRA.Text.Trim();
            var spell = _archive.FindSpellByCRA(cra);

            if (spell != null)
            {
                MessageBox.Show($"Trovato: {spell.Name}", "Risultato");
                // Potresti aprire direttamente la finestra di modifica:
                var win = new AddModifySpellWindow(_archive, spell);
                win.ShowDialog();
            }
            else
            {
                MessageBox.Show("Nessun incantesimo trovato.", "Errore");
            }
        }

        private void BtnOpenAddModify_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddModifySpellWindow(_archive, null);
            win.ShowDialog();
            UpdateRareCount();
        }

        private void BtnOpenSchools_Click(object sender, RoutedEventArgs e)
        {
            var win = new SchoolWindow();
            win.ShowDialog();
        }

        private void BtnOpenBySchool_Click(object sender, RoutedEventArgs e)
        {
            var win = new SpellsBySchoolWindow(_archive);
            win.ShowDialog();
        }
    }
}