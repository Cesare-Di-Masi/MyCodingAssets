namespace SpellArchiveLib
{
    public class Spell
    {
        private string _name, _description;
        private Wizard _wizard;
        private DateOnly _creationDate;
        private AccessLevel _accessibility;
        private List<SpellsSchool> _spellSchool;
        private int _dangerLevel;

        private string _cRA;

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Spell name cannot be null or empty.");
                }
                else
                {
                    _name = value;
                }
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Spell description cannot be null or empty.");
                }
                else
                {
                    _description = value;
                }
            }
        }

        public Wizard Wizard
        {
            get
            {
                return _wizard;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "New wizard cannot be null.");
                }
                if (value.Level < DangerLevel)
                {
                    throw new ArgumentException("New wizard level cannot lower than spell danger level.");
                }
                /*if (value.WizardSchool != SpellSchool)
                    {
                    throw new ArgumentException($"New wizard's school {value.WizardSchool} does not match the spell's school {SpellSchool}.");
                    }
                */
                else
                {
                    _wizard = value;
                }
            }
        }

        public DateOnly CreationDate
        {
            get
            {
                return _creationDate;
            }
            set
            {
                if (value > DateOnly.FromDateTime(DateTime.Now))
                {
                    throw new ArgumentException("Creation date cannot be in the future.");
                }
                else
                {
                    _creationDate = value;
                }
            }
        }

        public AccessLevel Accessibility
        {
            get
            {
                return _accessibility;
            }
            set
            {
                if (!Enum.IsDefined(typeof(AccessLevel), value))
                {
                    throw new ArgumentException("Invalid accessibility value.");
                }
                else
                {
                    _accessibility = value;
                }
            }
        }

        public List<SpellsSchool> SpellSchool
        {
            get
            {
                return _spellSchool;
            }
            set
            {
                for (int i = 0; i < value.Count; i++)
                {
                    if (!Enum.IsDefined(typeof(SpellsSchool), value[i]))
                    {
                        throw new ArgumentException($"Invalid spell school value: {value[i]}.");
                    }
                }
                value.Sort();
                _spellSchool = value;
            }
        }

        public int DangerLevel
        {
            get
            {
                return _dangerLevel;
            }
            set
            {
                if (value < 0 || value > 10)
                {
                    throw new ArgumentException("Danger level must be between 0 and 10.");
                }
                else
                {
                    _dangerLevel = value;
                }
            }
        }

        public string CRA
        {
            get
            {
                return _cRA;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("CRA cannot be null or empty.");
                }
                else
                {
                    _cRA = value;
                }
            }
        }

        public Spell(string name, string description, Wizard wizard, DateOnly creationDate, AccessLevel accessibility, int dangerLevel, List<SpellsSchool> spellSchool)
        {
            Name = name;
            Description = description;
            Wizard = wizard;
            CreationDate = creationDate;
            Accessibility = accessibility;
            SpellSchool = spellSchool;
            generateCRA();
        }

        public void generateCRA()
        {
            // Generate a unique CRA based on the spell's properties
            _cRA = $"{Name.Substring(0, 3).ToUpper()}-{CreationDate.Year}-{Wizard.Name.Substring(0, 3).ToUpper()}";
        }

        public void addSpellSchool(SpellsSchool school)
        {
            SpellSchool.Add(school);
        }

        public void ModifyName(string newName)
        {
            Name = newName;
            generateCRA(); // Regenerate CRA after modifying the name
        }

        public void ModifyDescription(string newDescription)
        {
            Description = newDescription;
        }

        public void ModifyWizard(Wizard newWizard)
        {
            Wizard = newWizard;
            generateCRA(); // Regenerate CRA after modifying the wizard
        }

        public void ModifyCreationDate(DateOnly newCreationDate)
        {
            CreationDate = newCreationDate;
            generateCRA(); // Regenerate CRA after modifying the creation date
        }

        public void ModifyAccessibility(AccessLevel newAccessibility)
        {
            Accessibility = newAccessibility;
        }

        public void ModifyDangerLevel(int newDangerLevel)
        {
            DangerLevel = newDangerLevel;
        }

        public void ModifySpell(Spell newSpell)
        {
            if (newSpell == null)
            {
                throw new ArgumentNullException(nameof(newSpell), "New spell cannot be null.");
            }
            ModifyName(newSpell.Name);
            ModifyDescription(newSpell.Description);
            ModifyWizard(newSpell.Wizard);
            ModifyCreationDate(newSpell.CreationDate);
            ModifyAccessibility(newSpell.Accessibility);
            ModifyDangerLevel(newSpell.DangerLevel);
            SpellSchool = newSpell.SpellSchool;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null || !(obj is Spell))
            {
                return false;
            }

            Spell other = (Spell)obj;

            if (CRA == other.CRA)
                return true;
            return false;
        }

        public override string ToString()
        {
            return $"Spell Name: {Name}, Description: {Description}, Wizard: {Wizard.Name} {Wizard.Surname}, " +
                 $"Creation Date: {CreationDate}, Accessibility: {Accessibility}, Danger Level: {DangerLevel}," +
                 $"Spell School(s):\n {string.Join(" ", SpellSchool, "\n")}, CRA: {CRA}";
        }
    }
}