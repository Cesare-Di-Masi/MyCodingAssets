using Domain.Model.ValueObjects;

namespace Domain.Model.Entities
{
    public class Cat
    {
        private string _name;
        private string _description;
        private Breed _breed;
        private bool _isMale;
        private DateOnly _arrivingDate;
        private DateOnly? _birthdate;
        private string _id;

        public string Name
        {
            get { return _name; }
            private set
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Name cannot be null or whitespace.", nameof(value));
                _name = value;
            }
        }

        public bool IsMale
        {
            get { return _isMale; }
            private set { _isMale = value; }
        }

        public DateOnly ArrivingDate
        {
            get { return _arrivingDate; }
            private set
            {
                if (value > DateOnly.FromDateTime(DateTime.Now))
                    throw new ArgumentException("Arriving date cannot be in the future.", nameof(value));
                _arrivingDate = value;
            }
        }

        public DateOnly? BirthDate
        {
            get { return _birthdate; }
            private set
            {
                if (value > DateOnly.FromDateTime(DateTime.Now))
                    throw new ArgumentException("Birth date cannot be in the future.", nameof(value));
                _birthdate = value;
            }
        }

        public Breed Breed
        {
            get { return _breed; }
            private set { _breed = value; }
        }

        public string? Description
        {
            get { return _description; }
            private set { _description = value ?? string.Empty; }
        }

        public string ID
        {
            get { return _id; }
            private set { _id = value; }
        }

        public Cat(string name, bool isMale, DateOnly arrivingDate, DateOnly? birthDate, Breed? breed, string? description, string? Id = null)
        {
            Name = name;
            IsMale = isMale;
            ArrivingDate = arrivingDate;
            BirthDate = birthDate;
            Breed = breed ?? new Breed("no breed");
            Description = description ?? "no description";
            if (Id != null || !string.IsNullOrWhiteSpace(Id)|| !string.IsNullOrEmpty(Id))
            {
                ID = Id;
            }
            else
                CalculateID();
        }

        public Cat()
        {
            Name = "Unnamed";
            IsMale = true;
            ArrivingDate = DateOnly.FromDateTime(DateTime.Now);
            BirthDate = null;
            Breed = new Breed("no breed");
            Description = "no description";
        }

        public override string ToString()
        {
            return $"{Name} is a {(IsMale ? "male" : "female")} cat, arrived on {ArrivingDate}.";
        }

        private void CalculateID()
        {/*
            Ogni gatto deve avere un codice identificativo univoco generato al momento dell'iscrizione. Questo codice sarà composto da:
            Un numero random di 5 cifre.
            La prima lettera del mese di registrazione.
            L'anno della data di registrazione.
            Tre lettere casuali.
          */
            Random random = new Random();
            int randomNumber = random.Next(10000, 99999);
            char monthLetter = ArrivingDate.ToString("MMMM")[0];
            int year = ArrivingDate.Year;
            string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string randomLetters = new string(Enumerable.Range(0, 3).Select(_ => letters[random.Next(letters.Length)]).ToArray());
            ID = $"{randomNumber}{monthLetter}{year}{randomLetters}";
        }

        public void ModifyBirthDate(DateOnly newBirthDate)
        {
            if (newBirthDate > DateOnly.FromDateTime(DateTime.Now))
                throw new ArgumentException("Birth date cannot be in the future.", nameof(newBirthDate));
            BirthDate = newBirthDate;
        }

        public void ModifyDescription(string newDescription)
        {
            Description = newDescription ?? "no description";
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Cat))
                return false;
            Cat other = (Cat)obj;
            return ID == other.ID;
        }
    }
}