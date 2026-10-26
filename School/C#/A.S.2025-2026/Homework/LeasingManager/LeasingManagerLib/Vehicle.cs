namespace LeasingManagerLib
{
    public abstract class Vehicle
    {
        public string PlateNumber { get; protected set; }
        public double PricePerDay { get; protected set; }

        public bool IsAvailable { get; protected set; } = true;

        public Vehicle(string plateNumber, double pricePerDay)
        {
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