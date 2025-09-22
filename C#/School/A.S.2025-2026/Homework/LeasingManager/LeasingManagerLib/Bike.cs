using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeasingManagerLib
{
    public class Bike : Vehicle
    {
        public Bike(string plateNumber, double pricePerDay) : base(plateNumber, pricePerDay)
        {
        }

        public override double totPrice(int days)
        {
            if (days > 7)
            {
                return PricePerDay * days * 0.9; // 10% discount for more than 7 days
            }
            else
            {
                return PricePerDay * days;
            }
        }

        public override string Description()
        {
            return $"Bike {PlateNumber}, costs {PricePerDay} per day.";
        }
    }
}