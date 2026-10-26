using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeasingManagerLib
{
    public class Motorbike : Vehicle
    {
        public HelmetTypes HelmetType { get; private set; }

        public Motorbike(string plateNumber, double pricePerDay, HelmetTypes helmetType, int discountPercent, int discountDays) : base(plateNumber, pricePerDay, discountPercent, discountDays)
        {
            HelmetType = helmetType;
        }

        public override string Description()
        {
            return $"Motorbike {PlateNumber} with a {HelmetType} helmet, costs {PricePerDay} per day.";
        }
    }
}