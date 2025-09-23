using LeasingManagerLib;

namespace LeasingSystemTest
{
    [TestClass]
    public sealed class VehicleTest
    {
        // Classe concreta per testare la classe astratta Vehicle
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

        [TestMethod]
        public void Constructor_ValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            string plate = "ABC123";
            double price = 50.0;
            int discountPercent = 10;
            int discountDays = 5;

            // Act
            var vehicle = new TestVehicle(plate, price, discountPercent, discountDays);

            // Assert
            Assert.AreEqual(plate, vehicle.PlateNumber);
            Assert.AreEqual(price, vehicle.PricePerDay);
            Assert.AreEqual(discountPercent, vehicle.DiscountPercent);
            Assert.AreEqual(discountDays, vehicle.DiscountDays);
            Assert.IsTrue(vehicle.IsAvailable);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_NullPlateNumber_ShouldThrowException()
        {
            // Arrange
            string plate = null;
            double price = 50.0;
            int discountPercent = 10;
            int discountDays = 5;

            // Act
            var vehicle = new TestVehicle(plate, price, discountPercent, discountDays);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_EmptyPlateNumber_ShouldThrowException()
        {
            // Arrange
            string plate = "";
            double price = 50.0;
            int discountPercent = 10;
            int discountDays = 5;

            // Act
            var vehicle = new TestVehicle(plate, price, discountPercent, discountDays);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_ZeroPricePerDay_ShouldThrowException()
        {
            // Arrange
            string plate = "ABC123";
            double price = 0.0;
            int discountPercent = 10;
            int discountDays = 5;

            // Act
            var vehicle = new TestVehicle(plate, price, discountPercent, discountDays);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_NegativePricePerDay_ShouldThrowException()
        {
            // Arrange
            string plate = "ABC123";
            double price = -10.0;
            int discountPercent = 10;
            int discountDays = 5;

            // Act
            var vehicle = new TestVehicle(plate, price, discountPercent, discountDays);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_NegativeDiscountPercent_ShouldThrowException()
        {
            // Arrange
            string plate = "ABC123";
            double price = 50.0;
            int discountPercent = -10;
            int discountDays = 5;

            // Act
            var vehicle = new TestVehicle(plate, price, discountPercent, discountDays);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_DiscountPercentOver100_ShouldThrowException()
        {
            // Arrange
            string plate = "ABC123";
            double price = 50.0;
            int discountPercent = 110;
            int discountDays = 5;

            // Act
            var vehicle = new TestVehicle(plate, price, discountPercent, discountDays);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_NegativeDiscountDays_ShouldThrowException()
        {
            // Arrange
            string plate = "ABC123";
            double price = 50.0;
            int discountPercent = 10;
            int discountDays = -5;

            // Act
            var vehicle = new TestVehicle(plate, price, discountPercent, discountDays);
        }

        [TestMethod]
        public void SetAvailability_True_ShouldSetIsAvailableToTrue()
        {
            // Arrange
            var vehicle = new TestVehicle("ABC123", 50.0, 10, 5);
            vehicle.SetAvailability(false); // Imposta a false prima del test

            // Act
            vehicle.SetAvailability(true);

            // Assert
            Assert.IsTrue(vehicle.IsAvailable);
        }

        [TestMethod]
        public void SetAvailability_False_ShouldSetIsAvailableToFalse()
        {
            // Arrange
            var vehicle = new TestVehicle("ABC123", 50.0, 10, 5);

            // Act
            vehicle.SetAvailability(false);

            // Assert
            Assert.IsFalse(vehicle.IsAvailable);
        }

        [TestMethod]
        public void Description_ShouldReturnCorrectFormat()
        {
            // Arrange
            var vehicle = new TestVehicle("ABC123", 50.0, 10, 5);

            // Act
            string description = vehicle.Description();

            // Assert
            Assert.AreEqual("Test Vehicle - Plate: ABC123", description);
        }
    }
}