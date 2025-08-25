using SpellArchiveLib;
using System.Windows;

namespace SpellArchive
{
    /// <summary>
    /// Logica di interazione per AddModifySpellWindow.xaml
    /// </summary>
    public partial class AddModifySpellWindow : Window
    {
        private Archive _archive;
        private Spell? _spell;

        private List<Wizard> wizards = new List<Wizard>()
            {
                new Wizard("Merlin","Ambrosius", new DateOnly(1000,1,1), 10){ WizardSchool = SpellsSchool.Astromancy },
                new Wizard("Gandalf","Grey", new DateOnly(1200,1,1), 9){ WizardSchool = SpellsSchool.Evocation }
            };

        public AddModifySpellWindow(Archive archive, Spell? spellToEdit)
        {
            InitializeComponent();
            _archive = archive;
            _spell = spellToEdit;

            // Popola i combobox
            CmbAccessibility.ItemsSource = Enum.GetValues(typeof(AccessLevel));
            CmbWizard.ItemsSource = wizards; // Qui userai i tuoi wizard reali
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
                AccessLevel acc = (AccessLevel)CmbAccessibility.SelectedItem;
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

                MainWindow a = new MainWindow(_archive);
                a.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Errore");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var win = new MainWindow(_archive);
            win.Show();
            this.Close();
        }

        // Dummy per test: crea 2 maghi finti
        private List<Wizard> GetDummyWizards()
        {
            /*List<String> wizardNames = new List<String>();

            for(int i = 0; i < wizards.Count; i++)
            {
                wizardNames.Add(wizards[i].ToString());
            }
            return wizardNames;*/
            return wizards;
        }
    }
}