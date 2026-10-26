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

        public Motorbike(string plateNumber, double pricePerDay, HelmetTypes helmetType) : base(plateNumber, pricePerDay)
        {
            HelmetType = helmetType;
        }

        public override double totPrice(int days)
        {
            return PricePerDay * days;
        }

        public override string Description()
        {
            return $"Motorbike {PlateNumber} with a {HelmetType} helmet, costs {PricePerDay} per day.";
        }
    }
}