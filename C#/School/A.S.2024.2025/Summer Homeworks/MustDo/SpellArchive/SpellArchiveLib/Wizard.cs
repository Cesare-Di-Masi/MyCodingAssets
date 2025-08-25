namespace SpellArchiveLib
{
    public class Wizard
    {
        private string _name, _surname;
        private DateOnly _birthDate;
        private SpellsSchool _wizardSchool;
        private int _level;
        private List<Spell?> _spells;

        public string Name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Name cannot be null or empty.");
                }
                _name = value;
            }
        }

        public string Surname
        {
            get { return _surname; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Surname cannot be null or empty.");
                }
                _surname = value;
            }
        }

        public DateOnly BirthDate
        {
            get { return _birthDate; }
            set
            {
                if (value > DateOnly.FromDateTime(DateTime.Now))
                {
                    throw new ArgumentException("Birth date cannot be in the future.");
                }
                _birthDate = value;
            }
        }

        public SpellsSchool WizardSchool
        {
            get { return _wizardSchool; }
            set
            {
                if (!Enum.IsDefined(typeof(SpellsSchool), value))
                {
                    throw new ArgumentException("Invalid school value.");
                }
                _wizardSchool = value;
            }
        }

        public int Level
        {
            get { return _level; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException("Level must be greater than 0.");
                }
                _level = value;
            }
        }

        public List<Spell?> Spells
        {
            get { return _spells; }
            set
            {
                _spells = value;
            }
        }

        public Wizard(string name, string surname, DateOnly birthDate, int level)
        {
            BirthDate = birthDate;
            Name = name;
            Surname = surname;
            Level = level;
            Spells = new List<Spell?>();
        }

        public void AddSpell(Spell spell)
        {
            if (spell == null)
            {
                throw new ArgumentNullException(nameof(spell), "Spell cannot be null.");
            }
            if (spell.DangerLevel > Level)
            {
                throw new ArgumentException($"Spell {spell.Name} has a danger level of {spell.DangerLevel}, which exceeds the wizard's level of {_level}.");
            }
            if (spell.Accessibility == AccessLevel.Forbidden)
            {
                throw new ArgumentException($"Spell {spell.Name} is forbidden and cannot be added to the wizard's spell list.");
            }
            if (spell.Accessibility == AccessLevel.Restricted && Level < 7)
            {
                throw new ArgumentException($"Spell {spell.Name} is restricted and requires a wizard level of at least 7 to be added to the spell list.");
            }

            for (int i = 0; i < _spells.Count; i++)
            {
                if (_spells[i] == spell)
                {
                    throw new ArgumentException($"Spell {spell.Name} already exists in the wizard's spell list.");
                }
            }
            _spells.Add(spell);
        }

        public void addSpells(List<Spell> spells)
        {
            for (int i = 0; i < spells.Count; i++)
            {
                AddSpell(spells[i]);
            }
        }

        public void RemoveSpell(Spell spell)
        {
            if (spell == null)
            {
                throw new ArgumentNullException(nameof(spell), "Spell cannot be null.");
            }
            if (!_spells.Contains(spell))
            {
                throw new ArgumentException($"Spell {spell.Name} does not exist in the wizard's spell list.");
            }
            _spells.Remove(spell);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Wizard))
                return false;

            Wizard other = (Wizard)obj;

            if (other.Name == Name &&
               other.Surname == Surname &&
               other.BirthDate == BirthDate &&
               other.WizardSchool == WizardSchool &&
               other.Level == Level)
            {
                if (other.Spells.Count != Spells.Count)
                    return false;
                for (int i = 0; i < Spells.Count; i++)
                {
                    if (Spells[i] != null && !Spells[i].Equals(other.Spells[i]))
                        return false;
                }
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"{Name} {Surname}";
        }
    }
}