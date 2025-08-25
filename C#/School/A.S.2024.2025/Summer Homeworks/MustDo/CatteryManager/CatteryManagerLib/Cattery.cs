using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatteryManagerLib
{
    public class Cattery
    {

        private List<Cat?> _cats;
        private List<Adopter?> _adopters;
        private List<Adoption?> _adoptions;
        private List<Adoption?> _canceledAdoption;

        public CatBreeds CatBreeds { get; set; }

        public List<Cat?> Cats
        {
            get { return _cats; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Cats list cannot be null.");
                }
                _cats = value;
            }
        }

        public List<Adopter?> Adopters
        {
            get { return _adopters; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Adopters list cannot be null.");
                }
                _adopters = value;
            }
        }

        public List<Adoption?> Adoptions
        {
            get { return _adoptions; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Adoptions list cannot be null.");
                }
                _adoptions = value;
            }
        }

        public List<Adoption?> CanceledAdoptions
        {
            get { return _canceledAdoption; }
            set
            {    
                _canceledAdoption = value;
            }
        }

        public Cattery()
        {
            Cats = new List<Cat?>();
            Adopters = new List<Adopter?>();
            Adoptions = new List<Adoption?>();
        }

        public Cattery(List<Cat?> cats):this()
        {
            Cats = cats;
        }

        public Cattery(List<Cat?> cats, List<Adopter?> adopters) : this(cats)
        {
            Adopters = adopters;
        }

        public Cattery(List<Cat?> cats, List<Adopter?> adopters, List<Adoption?> adoptions) : this(cats, adopters)
        {
            Adoptions = adoptions;
        }

        public void RemoveCat(Cat cat)
        {
            bool removed = Cats.Remove(cat);
            if(!removed)
            {
                throw new ArgumentException("The cat to be removed is not in the cattery.");
            }
            Update();
        }

        public void AddCat(Cat cat)
        {
            bool found = false;
            for (int i=0; i < Cats.Count; i++)
            {
                if(Cats[i].Equals(cat))
                {
                    found = true;
                }
            }
            if(found)
            {
                throw new ArgumentException("The cat to be added is already in the cattery.");
            }
            Cats.Add(cat);
            Update();
        }

        public void RegisterAdopter(Adopter adopter)
        {
            bool found = false;
            for (int i=0; i < Adopters.Count; i++)
            {
                if(Adopters[i].Equals(adopter))
                {
                    found = true;
                }
            }
            if(found)
            {
                throw new ArgumentException("The adopter to be registered is already in the cattery.");
            }
            Adopters.Add(adopter);
            Update();
        }

        public void RemoveAdopter(Adopter adopter)
        {
            bool found = false;

            for (int i = 0; i < Adopters.Count; i++)
            {
                if (Adopters[i].Equals(adopter))
                {
                    found = true;
                }
            }
            if(!found)
            {
                throw new ArgumentException("The adopter to be removed is not registered.");
            }
            found = false;
            for (int i=0; i < Adoptions.Count; i++)
            {
                if(Adoptions[i].Adopter.Equals(adopter))
                {
                    found = true;
                }
            }
            if(found)
            {
                throw new ArgumentException("The adopter to be removed has adoptions registered.");
            }
            Adopters.Remove(adopter);
            Update();
        }

        public void RegisterAdoption(Adoption adoption)
        {
            bool found = false;
            for (int i=0; i < Cats.Count; i++)
            {
                if(Cats[i].Equals(adoption.Cat))
                {
                    found = true;
                }
            }
            if(!found)
            {
                throw new ArgumentException("The cat to be adopted is not in the cattery.");
            }

            found = false;

            for (int i=0; i < Adopters.Count; i++)
            {
                if(Adopters[i].Equals(adoption.Adopter))
                {
                    found = true;
                }
            }
            if(!found)
            {
                Adopters.Add(adoption.Adopter);
            }

            Adoptions.Add(adoption);
            RemoveCat(adoption.Cat);
            Update();
        }

        public void CancelAdoption(Adoption adoption)
        {
            bool removed = Adoptions.Remove(adoption);
            if(!removed)
            {
                throw new ArgumentException("The adoption to be canceled is not registered.");
            }
            adoption.AdoptionCanceled();
            AddCat(adoption.Cat);
            CanceledAdoptions.Add(adoption);
            Adoptions.Remove(adoption);
            Update();
        }

        public void Update()
        {
            Serializer.SaveToFile(this);
        }


    }
}
