namespace LeasingManagerLib
{
    public abstract class Vehicle
    {
        public string PlateNumber { get; protected set; }
        public double PricePerDay { get; protected set; }

        public bool IsAvailable { get; protected set; } = true;

        public Vehicle(string plateNumber, double pricePerDay)
        {

            if(String.isWhiteOrNull(plateNumber)==true)
                throw new ArgumentException("illegal numberplate")
            
            if(pricePerDay <= 0.0)
                throw new ArgumentException("illegal price per day")

            PlateNumber = plateNumber;
            PricePerDay = pricePerDay;
        }

        public void SetAvailability(bool availability)
        {
            IsAvailable = availability;
        }

        public abstract double totPrice(int days);

        public abstract string Description();
    }
}