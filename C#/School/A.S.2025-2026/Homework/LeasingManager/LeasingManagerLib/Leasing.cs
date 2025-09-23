using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeasingManagerLib
{
    public class Leasing
    {
        private Vehicle _vehicle;
        private int _leaseDays;

        public Vehicle Vehicle
        {
            get { return _vehicle; }
            private set
            {
                if (value == null)
                    throw new ArgumentNullException("Vehicle cannot be null.");
                _vehicle = value;
            }
        }

        public int LeaseDays
        {
            get { return _leaseDays; }
            private set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("Lease days must be greater than zero.");
                _leaseDays = value;
            }
        }

        public Leasing(Vehicle vehicle, int leaseDays)
        {
            _vehicle = vehicle;
            _leaseDays = leaseDays;
        }

        public double CalculateTotalPrice()
        {
            double price = Vehicle.PricePerDay * LeaseDays;
            if (Vehicle.DiscountPercent > 0 && LeaseDays >= Vehicle.DiscountDays)
                price -= Vehicle.DiscountPercent * price / 100;
            return price;
        }
    }
}