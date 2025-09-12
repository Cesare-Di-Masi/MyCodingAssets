namespace BossGeneratorLib
{
    public class WeaponRoster
    {
        private List<Weapon?> _weapons; //tutte le armi di questo tipo
        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value not acceptable");
                _name = value;
            }
        }

        public List<Weapon?> Weapons
        {
            get { return _weapons; }
        }

        public WeaponRoster(string name)
        {
            _name = name;
        }

        public void AddWeapon(Weapon weapon)
        {
            _weapons.Add(weapon);
        }
    }
}