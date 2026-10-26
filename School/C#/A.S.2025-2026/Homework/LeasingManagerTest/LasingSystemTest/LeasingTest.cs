using LeasingManagerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeasingSystemTest
{
    [TestClass]
    public class LeasingTest
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

        [TestMethod]
        public void Constructor_ValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            var vehicle = new TestVehicle("ABC123", 50.0, 10, 5);
            int leaseDays = 10;

            // Act
            var leasing = new Leasing(vehicle, leaseDays);

            // Assert
            Assert.AreEqual(vehicle, leasing.Vehicle);
            Assert.AreEqual(leaseDays, leasing.LeaseDays);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullVehicle_ShouldThrowException()
        {
            // Arrange
            Vehicle vehicle = null;
            int leaseDays = 10;

            // Act
            var leasing = new Leasing(vehicle, leaseDays);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_ZeroLeaseDays_ShouldThrowException()
        {
            // Arrange
            var vehicle = new TestVehicle("ABC123", 50.0, 10, 5);
            int leaseDays = 0;

            // Act
            var leasing = new Leasing(vehicle, leaseDays);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_NegativeLeaseDays_ShouldThrowException()
        {
            // Arrange
            var vehicle = new TestVehicle("ABC123", 50.0, 10, 5);
            int leaseDays = -5;

            // Act
            var leasing = new Leasing(vehicle, leaseDays);
        }

        [TestMethod]
        public void CalculateTotalPrice_NoDiscount_ShouldReturnCorrectPrice()
        {
            // Arrange
            var vehicle = new TestVehicle("ABC123", 50.0, 0, 5); // Nessuno sconto
            int leaseDays = 10;
            var leasing = new Leasing(vehicle, leaseDays);
            double expectedPrice = 50.0 * 10; // 500

            // Act
            double actualPrice = leasing.CalculateTotalPrice();

            // Assert
            Assert.AreEqual(expectedPrice, actualPrice);
        }

        [TestMethod]
        public void CalculateTotalPrice_WithDiscountDaysNotReached_ShouldReturnPriceWithoutDiscount()
        {
            // Arrange
            var vehicle = new TestVehicle("ABC123", 50.0, 10, 15); // Sconto del 10% dopo 15 giorni
            int leaseDays = 10; // Meno dei giorni necessari per lo sconto
            var leasing = new Leasing(vehicle, leaseDays);
            double expectedPrice = 50.0 * 10; // 500

            // Act
            double actualPrice = leasing.CalculateTotalPrice();

            // Assert
            Assert.AreEqual(expectedPrice, actualPrice);
        }

        [TestMethod]
        public void CalculateTotalPrice_WithDiscountDaysReached_ShouldReturnPriceWithDiscount()
        {
            // Arrange
            var vehicle = new TestVehicle("ABC123", 50.0, 10, 5); // Sconto del 10% dopo 5 giorni
            int leaseDays = 10; // Più dei giorni necessari per lo sconto
            var leasing = new Leasing(vehicle, leaseDays);
            double expectedPrice = 50.0 * 10 * 0.9; // 450 (10% di sconto)

            // Act
            double actualPrice = leasing.CalculateTotalPrice();

            // Assert
            Assert.AreEqual(expectedPrice, actualPrice);
        }

        [TestMethod]
        public void CalculateTotalPrice_WithDiscountDaysExactlyReached_ShouldReturnPriceWithDiscount()
        {
            // Arrange
            var vehicle = new TestVehicle("ABC123", 50.0, 10, 5); // Sconto del 10% dopo 5 giorni
            int leaseDays = 5; // Esattamente i giorni necessari per lo sconto
            var leasing = new Leasing(vehicle, leaseDays);
            double expectedPrice = 50.0 * 5 * 0.9; // 225 (10% di sconto)

            // Act
            double actualPrice = leasing.CalculateTotalPrice();

            // Assert
            Assert.AreEqual(expectedPrice, actualPrice);
        }
    }
}