using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeasingManagerLib
{
    public class LeasingSystem
    {
        public List<Vehicle> Vehicles { get; private set; }

        public LeasingSystem()
        {
            Vehicles = new List<Vehicle>();
        }

        public LeasingSystem(List<Vehicle> vehicles)
        {
            Vehicles = vehicles;
        }

        public void AddVehicle(Vehicle vehicle)
        {
            Vehicles.Add(vehicle);
        }

        public Vehicle? FindVehicle(string plateNumber)
        {
            return Vehicles.FirstOrDefault(v => v.PlateNumber == plateNumber);
        }

        public double CalculateTotalPrice(string plateNumber, int days)
        {
            var vehicle = FindVehicle(plateNumber);
            if (vehicle == null)
            {
                throw new ArgumentNullException("Vehicle not found");
            }
            return vehicle.totPrice(days);
        }

        public string GetVehicleDescription(string plateNumber)
        {
            var vehicle = FindVehicle(plateNumber);
            if (vehicle == null)
            {
                throw new ArgumentNullException("Vehicle not found");
            }
            return vehicle.Description();
        }

        public void LeaseVehicle(string plateNumber)
        {
            var vehicle = FindVehicle(plateNumber);
            if (vehicle == null)
            {
                throw new ArgumentNullException("Vehicle not found");
            }
            if (!vehicle.IsAvailable)
            {
                throw new InvalidOperationException("Vehicle is not available for lease");
            }
            vehicle.SetAvailability(false);
        }

        public void ReturnVehicle(string plateNumber)
        {
            var vehicle = FindVehicle(plateNumber);
            if (vehicle == null)
            {
                throw new ArgumentNullException("Vehicle not found");
            }
            vehicle.SetAvailability(true);
        }
    }
}