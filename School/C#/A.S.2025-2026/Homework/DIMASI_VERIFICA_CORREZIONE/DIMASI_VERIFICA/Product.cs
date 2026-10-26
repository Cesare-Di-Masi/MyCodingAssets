namespace DIMASI_VERIFICA
{
    public class Product
    {
        private string _id, _name;
        private double _price;

        public string ID
        {
            get { return _id; }
            set
            {
                if (string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("illegal ID for product");
                }
                _id = value;
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("illegal Name for product");
                }
                _name = value;
            }
        }

        public double Price
        {
            get { return _price; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("illegal price for product");
                _price = value;
            }
        }

        public Product(string id, string name, double price)
        {
            ID = id;
            Name = name;
            Price = price;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode(); //ritorno l'hash code della string come identificatore univoco dell'oggetto
        }

        public override string ToString()
        {
            return $"{ID}: name:{Name}";
        }
    }
}