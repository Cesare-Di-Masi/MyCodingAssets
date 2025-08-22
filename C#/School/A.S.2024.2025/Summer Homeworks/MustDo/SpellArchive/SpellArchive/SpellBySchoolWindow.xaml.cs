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
