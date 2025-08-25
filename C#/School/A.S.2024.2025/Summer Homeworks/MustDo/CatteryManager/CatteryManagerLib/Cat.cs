using System.Text;

namespace CatteryManagerLib
{
    public class Cat
    {
        private string _iD, _name;
        private CatBreeds _race;
        private bool _isMale;
        private DateOnly _arriveDate;
        private DateOnly? _exitDate, _birthDate;

        public string ID
        {
            get
            {
                return _iD;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("ID cannot be null or empty.");
                }
                _iD = value;
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Name cannot be null or empty.");
                }
                _name = value;
            }
        }

        public CatBreeds Breed
        {
            get
            {
                return _race;
            }
            set
            {
                if (!(value is CatBreeds))
                {
                    throw new ArgumentException("illegal cat breed selected");
                }
                _race = value;
            }
        }

        public bool IsMale
        {
            get { return _isMale; }
            set { _isMale = value; }
        }

        public DateOnly ArriveDate
        {
            get { return _arriveDate; }
            set
            {
                if (value > DateOnly.FromDateTime(DateTime.Now))
                {
                    throw new ArgumentException("Arrive date cannot be in the future.");
                }
                _arriveDate = value;
            }
        }

        public DateOnly? ExitDate
        {
            get { return _exitDate; }
            set
            {
                if (value != null && value < _arriveDate)
                {
                    throw new ArgumentException("Exit date cannot be before arrive date.");
                }
                _exitDate = value;
            }
        }

        public DateOnly? BirthDate
        {
            get { return _birthDate; }
            set
            {
                if (value != null && value > DateOnly.FromDateTime(DateTime.Now))
                {
                    throw new ArgumentException("Birth date cannot be in the future.");
                }
                _birthDate = value;
            }
        }

        public Cat(string name, CatBreeds race, bool isMale, DateOnly arriveDate, DateOnly? exitDate, DateOnly? birthDate)
        {
            Name = name;
            Breed = race;
            IsMale = isMale;
            ArriveDate = arriveDate;
            ExitDate = exitDate;
            BirthDate = birthDate;
            GenerateID();
        }

        public string GenerateID()
        {
            Random rnd = new Random();

            // Numero random di 5 cifre
            int RNG = rnd.Next(10000, 100000);

            // Prima lettera del mese
            string[] mesi = { "G", "F", "M", "A", "M", "G", "L", "A", "S", "O", "N", "D" };
            string letteraMese = mesi[ArriveDate.Month - 1];

            // Anno
            string anno = ArriveDate.Year.ToString();

            // Tre lettere casuali derivanti dai valori del gatto
            string input = $"{Name}{Breed}{IsMale}{(BirthDate?.ToString("yyyyMMdd") ?? "XXXX")}";
            byte[] bytes = Encoding.ASCII.GetBytes(input);

            // Prendi 3 valori pseudo-casuali dai byte
            char l1 = (char)('A' + (bytes[0] % 26));
            char l2 = (char)('A' + (bytes.Length > 1 ? bytes[1] % 26 : rnd.Next(26)));
            char l3 = (char)('A' + (bytes.Length > 2 ? bytes[2] % 26 : rnd.Next(26)));

            return $"{RNG}{letteraMese}{anno}{l1}{l2}{l3}";
        }

        public void GettingAdopted()
        {
            ExitDate = DateOnly.FromDateTime(DateTime.Now);
        }

        public void GettingAdopted(DateOnly exitDate)
        {
            ExitDate = exitDate;
        }

        public void GettingBirthDate(DateOnly birthDate)
        {
            BirthDate = birthDate;
        }

        public void AdoptionCanceled()
        {
            ExitDate = null;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Cat))
            {
                return false;
            }

            Cat other = (Cat)obj;

            if (this.ID == other.ID)
            {
                return true;
            }
            return false;

        }

        public override string ToString()
        {
            return $"Name:{Name} Breed:{Breed} IsMale:{IsMale} BirthDate:{BirthDate} Arriving Date:{ArriveDate}";
        }
    }
}
