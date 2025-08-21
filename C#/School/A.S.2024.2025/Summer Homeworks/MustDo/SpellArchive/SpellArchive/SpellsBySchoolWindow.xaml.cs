using System.Windows;
using SpellArchiveLib;

namespace SpellArchive
{
    public partial class SpellsBySchoolWindow : Window
    {
        private Archive _archive;

        public SpellsBySchoolWindow(Archive archive)
        {
            InitializeComponent();
            _archive = archive;
            CmbSchools.ItemsSource = System.Enum.GetValues(typeof(SpellsSchool));
        }

        private void BtnLoadSpells_Click(object sender, RoutedEventArgs e)
        {
            if (CmbSchools.SelectedItem is SpellsSchool school)
            {
                var spells = _archive.FindSpellsBySchool(school);
                LstSpells.ItemsSource = spells;
            }
        }
    }
}
