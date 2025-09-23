using LeasingManagerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeasingSystemTest
{
    public class TestLeasingSystem
    {
        private class TestVehicle : Vehicle
        {
            public TestVehicle(string plateNumber, double pricePerDay, int discountPercent, int discountDays)
                : base(plateNumber, pricePerDay, discountPercent, discountDays)
            {
            }

            public override string Description()
            {
                return $"Test Vehicle - Plate: {PlateNumber}";
            }
        }

        private LeasingSystem _leasingSystem;
        private TestVehicle _testVehicle;

        [TestInitialize]
        public void Setup()
        {
            _leasingSystem = new LeasingSystem();
            _testVehicle = new TestVehicle("ABC123", 50.0, 10, 5);
        }

        [TestMethod]
        public void Constructor_Default_ShouldInitializeEmptyVehiclesList()
        {
            // Assert
            Assert.IsNotNull(_leasingSystem.Vehicles);
            Assert.AreEqual(0, _leasingSystem.Vehicles.Count);
        }

        [TestMethod]
        public void Constructor_WithVehicles_ShouldInitializeWithProvidedVehicles()
        {
            // Arrange
            var vehicles = new List<Vehicle> { _testVehicle };

            // Act
            var system = new LeasingSystem(vehicles);

            // Assert
            Assert.IsNotNull(system.Vehicles);
            Assert.AreEqual(1, system.Vehicles.Count);
            Assert.AreSame(_testVehicle, system.Vehicles[0]);
        }

        [TestMethod]
        public void AddVehicle_ShouldAddVehicleToList()
        {
            // Act
            _leasingSystem.AddVehicle(_testVehicle);

            // Assert
            Assert.AreEqual(1, _leasingSystem.Vehicles.Count);
            Assert.AreSame(_testVehicle, _leasingSystem.Vehicles[0]);
        }

        [TestMethod]
        public void FindVehicle_ExistingPlateNumber_ShouldReturnVehicle()
        {
            // Arrange
            _leasingSystem.AddVehicle(_testVehicle);

            // Act
            var result = _leasingSystem.FindVehicle("ABC123");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreSame(_testVehicle, result);
        }

        [TestMethod]
        public void FindVehicle_NonExistingPlateNumber_ShouldReturnNull()
        {
            // Arrange
            _leasingSystem.AddVehicle(_testVehicle);

            // Act
            var result = _leasingSystem.FindVehicle("NONEXIST");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetVehicleDescription_ExistingVehicle_ShouldReturnDescription()
        {
            // Arrange
            _leasingSystem.AddVehicle(_testVehicle);

            // Act
            string description = _leasingSystem.GetVehicleDescription("ABC123");

            // Assert
            Assert.AreEqual("Test Vehicle - Plate: ABC123", description);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetVehicleDescription_NonExistingVehicle_ShouldThrowException()
        {
            // Arrange
            _leasingSystem.AddVehicle(_testVehicle);

            // Act
            _leasingSystem.GetVehicleDescription("NONEXIST");
        }

        [TestMethod]
        public void LeaseVehicle_AvailableVehicle_ShouldSetUnavailableAndAddLeasing()
        {
            // Arrange
            _leasingSystem.AddVehicle(_testVehicle);

            // Act
            _leasingSystem.LeaseVehicle("ABC123", 3);

            // Assert
            Assert.IsFalse(_testVehicle.IsAvailable);
            Assert.AreEqual(1, _leasingSystem.Leasings.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void LeaseVehicle_NonExistingVehicle_ShouldThrowException()
        {
            // Act
            _leasingSystem.LeaseVehicle("NONEXIST", 3);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void LeaseVehicle_UnavailableVehicle_ShouldThrowException()
        {
            // Arrange
            _leasingSystem.AddVehicle(_testVehicle);

            _testVehicle.SetAvailability(false);

            // Act
            _leasingSystem.LeaseVehicle("ABC123", 3);
        }

        [TestMethod]
        public void ReturnVehicle_LeasedVehicle_ShouldSetAvailable()
        {
            // Arrange
            _leasingSystem.AddVehicle(_testVehicle);
            _testVehicle.SetAvailability(false);

            // Act
            _leasingSystem.ReturnVehicle("ABC123");

            // Assert
            Assert.IsTrue(_testVehicle.IsAvailable);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ReturnVehicle_NonExistingVehicle_ShouldThrowException()
        {
            // Act
            _leasingSystem.ReturnVehicle("NONEXIST");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ReturnVehicle_AlreadyAvailableVehicle_ShouldThrowException()
        {
            // Arrange
            _leasingSystem.AddVehicle(_testVehicle);

            // Act
            _leasingSystem.ReturnVehicle("ABC123");
        }
    }
}