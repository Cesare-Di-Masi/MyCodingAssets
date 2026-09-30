using Lessons3Y.FleetManagement.Domain.ValueObjects;

namespace Lessons3Y.FleetManagement.Domain
{
    /// <summary>
    /// Represents a vehicle in the fleet management system, including its properties and behaviors.
    /// </summary>
    public class Vehicle
    {
        public Guid Id { get; }
        public LicensePlate LicensePlate { get; } // Value Object
        public double OdometerKm { get; private set; }
        public Money DailyRate { get; private set; } // Value Object
        public double FuelLevelPercentage { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Vehicle class with the specified properties, performing validation on the input parameters.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="licensePlate"></param>
        /// <param name="initialKm"></param>
        /// <param name="dailyRate"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public Vehicle(Guid id, LicensePlate licensePlate, double initialKm, Money dailyRate)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("L'ID del veicolo non può essere vuoto.", nameof(id));

            if (initialKm < 0)
                throw new ArgumentException("Il chilometraggio iniziale non può essere negativo.", nameof(initialKm));

            Id = id;
            //licensePlate ?? --> if licensePlate is null, throw an exception. licensePlate is a value object, so we want to ensure it's not null when creating a Vehicle instance. 
            //licensePlate is null if the caller of the constructor passes null for the licensePlate parameter.
            LicensePlate = licensePlate ?? throw new ArgumentNullException(nameof(licensePlate));
            OdometerKm = initialKm;
            DailyRate = dailyRate ?? throw new ArgumentNullException(nameof(dailyRate));
            FuelLevelPercentage = 100.0;

            /*
            //potremmo scrivere il controlli anche in questo modo:
            ArgumentNullException.ThrowIfNull(licensePlate);
            ArgumentNullException.ThrowIfNull(dailyRate);
            */
        }

        /// <summary>
        /// Records a trip for the vehicle, updating the odometer and fuel level based on the distance driven and fuel consumed percentage.
        /// </summary>
        /// <param name="kilometersDriven"></param>
        /// <param name="fuelConsumedPercentage"></param>
        /// <exception cref="ArgumentException"></exception>
        public void RecordTrip(double kilometersDriven, double fuelConsumedPercentage)
        {
            if (kilometersDriven <= 0)
                throw new ArgumentException("I chilometri percorsi devono essere maggiori di zero.", nameof(kilometersDriven));

            if (fuelConsumedPercentage < 0 || fuelConsumedPercentage > 100)
                throw new ArgumentException("Il carburante consumato deve essere tra 0 e 100.", nameof(fuelConsumedPercentage));

            OdometerKm += kilometersDriven;
            FuelLevelPercentage = Math.Max(0.0, FuelLevelPercentage - fuelConsumedPercentage);
        }
        
        /// <summary>
        /// Refuels the vehicle, updating the fuel level based on the percentage added.
        /// </summary>
        /// <param name="percentageAdded"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Refuel(double percentageAdded)
        {
            if (percentageAdded <= 0)
                throw new ArgumentException("La percentuale da aggiungere deve essere maggiore di zero.", nameof(percentageAdded));

            FuelLevelPercentage = Math.Min(100.0, FuelLevelPercentage + percentageAdded);
        }

        //TODO: implementare i metodi per verificare se il veicolo è in riserva IsInReserve() (fuel level < 15%) e se è pieno IsFullTank (fuel level > 99% )

    }
}
