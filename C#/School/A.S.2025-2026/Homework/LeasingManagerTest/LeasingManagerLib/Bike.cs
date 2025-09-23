using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeasingManagerLib
{
    public class Bike : Vehicle
    {
        public Bike(string plateNumber, double pricePerDay, int discountPercent, int discountDays) : base(plateNumber, pricePerDay, discountPercent, discountDays)
        {
        }

        public override string Description()
        {
            return $"Bike {PlateNumber}, costs {PricePerDay} per day.";
        }
    }
}