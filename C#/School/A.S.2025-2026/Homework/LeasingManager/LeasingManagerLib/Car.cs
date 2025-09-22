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

        public Car(string plateNumber, double pricePerDay, int seatNumber) : base(plateNumber, pricePerDay)
        {
            SeatNumber = seatNumber;
        }

        public override double totPrice(int days)
        {
            return PricePerDay * days;
        }

        public override string Description()
        {
            return $"Car {PlateNumber} with {SeatNumber} seats, costs {PricePerDay} per day.";
        }
    }
}