using SpellArchiveLib;
using System.Windows;

namespace SpellArchive
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Archive _archive;
        private int forbCounter = 0; //numero di magie proibite (mia versione  di rare)

        public MainWindow()
        {
            InitializeComponent();
            _archive = Serializer.GetFile();
            if (_archive == null)
            {
                _archive = new Archive();
            }
            UpdateRareCount();
        }

        public MainWindow(Archive archive)
        {
            InitializeComponent();
            _archive = archive;
            UpdateRareCount();
        }

        private void UpdateRareCount()
        {
            for (int i = 0; i < _archive.SpellArchive.Count; i++)
            {
                if (_archive.SpellArchive[i] != null && _archive.SpellArchive[i]?.Accessibility == AccessLevel.Forbidden)
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
            win.Show();
            this.Close();
        }

        private void BtnOpenSchools_Click(object sender, RoutedEventArgs e)
        {
            var win = new SchoolWindow();
            win.Show();
            this.Close();
        }

        private void BtnOpenBySchool_Click(object sender, RoutedEventArgs e)
        {
            var win = new SpellBySchoolWindow(_archive);
            win.Show();
            this.Close();
        }
    }
}