using SpellArchiveLib;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpellArchive
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Archive _archive;
        private int forbCounter = 0; //numero di magie proibite (mia versione  di rare)
        public MainWindow(Archive archive)
        {
            InitializeComponent();
            _archive = archive;
            UpdateRareCount();
        }

        private void UpdateRareCount()
        {
            for(int i = 0; i < _archive.SpellArchive.Count; i++)
            {
                if(_archive.SpellArchive[i] != null && _archive.SpellArchive[i]?.Accessibility == Accessibility.Forbidden)
                {
                    forbCounter++;
                }
            }
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