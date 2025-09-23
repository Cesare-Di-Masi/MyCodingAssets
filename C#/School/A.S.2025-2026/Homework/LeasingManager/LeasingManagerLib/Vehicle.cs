namespace LeasingManagerLib
{
    public abstract class Vehicle
    {
        public string PlateNumber { get; protected set; }
        public double PricePerDay { get; protected set; }

        public bool IsAvailable { get; protected set; } = true;

        public int DiscountPercent
        { get; protected set; } = 0;

        public int DiscountDays
        { get; protected set; } = 0;

        public Vehicle(string plateNumber, double pricePerDay, int discountPercent, int discountDays)
        {
            if (string.IsNullOrEmpty(plateNumber) == true)
                throw new ArgumentException("illegal numberplate");

            if (pricePerDay <= 0.0)
                throw new ArgumentException("illegal price per day");

            if (discountPercent < 0 || discountPercent > 100)
                throw new ArgumentException("illegal discount percent");

            if (discountDays < 0)
                throw new ArgumentException("illegal discount days");

            PlateNumber = plateNumber;
            PricePerDay = pricePerDay;
            DiscountPercent = discountPercent;
            DiscountDays = discountDays;
        }

        public void SetAvailability(bool availability)
        {
            IsAvailable = availability;
        }

        public abstract string Description();
    }
}