using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeasingManagerLib
{
    public class Car : Vehicle
    {
        public int SeatNumber { get; private set; }

        public Car(string plateNumber, double pricePerDay, int seatNumber, int discountPercent, int discountDays) : base(plateNumber, pricePerDay, discountPercent, discountDays)
        {
            if (seatNumber < 1)
                throw new ArgumentException("illegal car seats");
            SeatNumber = seatNumber;
        }

        public override string Description()
        {
            return $"Car {PlateNumber} with {SeatNumber} seats, costs {PricePerDay} per day.";
        }
    }
}