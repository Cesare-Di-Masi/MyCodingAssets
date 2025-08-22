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
    /// Logica di interazione per AddModifySpellWindow.xaml
    /// </summary>
    public partial class AddModifySpellWindow : Window
    {
        private Archive _archive;
        private Spell? _spell;

        public AddModifySpellWindow(Archive archive, Spell? spellToEdit)
        {
            InitializeComponent();
            _archive = archive;
            _spell = spellToEdit;

            // Popola i combobox
            CmbAccessibility.ItemsSource = Enum.GetValues(typeof(Accessibility));
            CmbWizard.ItemsSource = GetDummyWizards(); // Qui userai i tuoi wizard reali
            LstSchools.ItemsSource = Enum.GetValues(typeof(SpellsSchool));

            if (_spell != null)
            {
                LoadSpellData(_spell);
            }
        }

        private void LoadSpellData(Spell spell)
        {
            TxtName.Text = spell.Name;
            TxtDescription.Text = spell.Description;
            CmbWizard.SelectedItem = spell.Wizard;
            DpCreationDate.SelectedDate = spell.CreationDate.ToDateTime(new TimeOnly(0, 0));
            CmbAccessibility.SelectedItem = spell.Accessibility;
            SldDanger.Value = spell.DangerLevel;

            foreach (var school in spell.SpellSchool)
            {
                LstSchools.SelectedItems.Add(school);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = TxtName.Text.Trim();
                string description = TxtDescription.Text.Trim();
                Wizard wizard = (Wizard)CmbWizard.SelectedItem;
                DateOnly creationDate = DateOnly.FromDateTime(DpCreationDate.SelectedDate ?? DateTime.Now);
                Accessibility acc = (Accessibility)CmbAccessibility.SelectedItem;
                int danger = (int)SldDanger.Value;
                List<SpellsSchool> schools = new List<SpellsSchool>();
                foreach (var s in LstSchools.SelectedItems)
                    schools.Add((SpellsSchool)s);

                if (_spell == null)
                {
                    Spell newSpell = new Spell(name, description, wizard, creationDate, acc, danger, schools);
                    _archive.AddSpell(newSpell);
                }
                else
                {
                    Spell updated = new Spell(name, description, wizard, creationDate, acc, danger, schools);
                    _archive.ModifySpell(_spell, updated);
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Errore");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Dummy per test: crea 2 maghi finti
        private List<Wizard> GetDummyWizards()
        {
            return new List<Wizard>
            {
                new Wizard("Merlin","Ambrosius", new DateOnly(1000,1,1), 10){ WizardSchool = SpellsSchool.Arcane },
                new Wizard("Gandalf","Grey", new DateOnly(1200,1,1), 9){ WizardSchool = SpellsSchool.Elemental }
            };
        }
    }
}
