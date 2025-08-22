using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellArchiveLib
{
    public class Archive
    {
        private List<Spell?> _spellArchive;

        public List<Spell?> SpellArchive
        {
            get { return _spellArchive; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "Spell archive cannot be null.");
                }
                _spellArchive = value;
            }
        }

        public Archive(List<Spell?> spellArchive)
        {
            SpellArchive = spellArchive;
        }

        public Archive()
        {
            SpellArchive = new List<Spell?>();
        }

        public void AddSpell(Spell spell)
        {
            if (spell == null)
            {
                throw new ArgumentNullException(nameof(spell), "Spell cannot be null.");
            }
            SpellArchive.Add(spell);
            Serializer.SaveToFile(this);
        }

        public void RemoveSpell(Spell spell)
        {
            if (spell == null)
            {
                throw new ArgumentNullException(nameof(spell), "Spell cannot be null.");
            }
            if (!SpellArchive.Contains(spell))
            {
                throw new ArgumentException("Spell not found in the archive.");
            }
            SpellArchive.Remove(spell);
            Serializer.SaveToFile(this); // Salva l'archivio dopo la rimozione
        }

        public Spell? FindSpellByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Spell name cannot be null or empty.", nameof(name));
            }
            return SpellArchive.FirstOrDefault(s => s?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
        }

        public List<Spell?> FindSpellsBySchool(SpellsSchool school)
        {
            List<Spell?> searched = new List<Spell?>();
            if (school == null)
            {
                throw new ArgumentNullException(nameof(school), "Spell school cannot be null.");
            }

            for (int i = 0; i < SpellArchive.Count; i++)
            {
                for (int j = 0; j < SpellArchive[i]?.SpellSchool.Count; j++)
                {
                    if (SpellArchive[i]?.SpellSchool[j] == school)
                    {
                        searched.Add(SpellArchive[i]);
                        break; // No need to check other schools for this spell
                    }
                }
            }
            return searched;
        }

        public List<Spell?> FindSpellsByAccessibility(Accessibility accessibility)
        {
            if (accessibility == null)
            {
                throw new ArgumentNullException(nameof(accessibility), "Accessibility cannot be null.");
            }
            return SpellArchive.Where(s => s?.Accessibility == accessibility).ToList();
        }

        public List<Spell?> FindSpellsByDangerLevel(int dangerLevel)
        {
            if (dangerLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dangerLevel), "Danger level cannot be negative.");
            }
            return SpellArchive.Where(s => s?.DangerLevel == dangerLevel).ToList();
        }

        public Spell? FindSpellByCRA(string cra)
        {
            if (string.IsNullOrWhiteSpace(cra))
            {
                throw new ArgumentException("CRA cannot be null or empty.", nameof(cra));
            }
            return SpellArchive.FirstOrDefault(s => s?.CRA.Equals(cra, StringComparison.OrdinalIgnoreCase) == true);
        }

        public List<Spell?> FindSpellsByWizard(Wizard wizard)
        {
            if (wizard == null)
            {
                throw new ArgumentNullException(nameof(wizard), "Wizard cannot be null.");
            }
            return SpellArchive.Where(s => s?.Wizard == wizard).ToList();
        }

        public List<Spell?> FindSpellsByCreationDate(DateOnly creationDate)
        {
            return SpellArchive.Where(s => s?.CreationDate == creationDate).ToList();
        }

        public void ModifySpell(Spell oldSpell, Spell newSpell)
        {
            if (oldSpell == null || newSpell == null)
            {
                throw new ArgumentNullException("Old spell or new spell cannot be null.");
            }
            int index = SpellArchive.IndexOf(oldSpell);
            if (index == -1)
            {
                throw new ArgumentException("Old spell not found in the archive.");
            }
            SpellArchive[index] = newSpell;
            Serializer.SaveToFile(this);
        }
    }
}
