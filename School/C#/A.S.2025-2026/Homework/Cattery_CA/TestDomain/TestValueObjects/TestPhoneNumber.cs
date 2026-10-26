using Domain.Model.ValueObjects;

namespace TestDomain.TestValueObjects
{
    [TestClass]
    public class TestPhoneNumber
    {
        [TestMethod]
        public void PhoneNumber_InvalidValues_ShouldThrow()
        {
            PhoneNumber phoneNumber;
            Assert.ThrowsException<ArgumentException>(() => phoneNumber = new PhoneNumber(null));
            Assert.ThrowsException<ArgumentException>(() => phoneNumber = new PhoneNumber("   "));
            Assert.ThrowsException<ArgumentException>(() => phoneNumber = new PhoneNumber("12345")); // Too short
            Assert.ThrowsException<ArgumentException>(() => phoneNumber = new PhoneNumber("1234567890123456")); // Too long
            Assert.ThrowsException<ArgumentException>(() => phoneNumber = new PhoneNumber("123-456-7890")); // Invalid characters
            Assert.ThrowsException<ArgumentException>(() => phoneNumber = new PhoneNumber("(123) 456-7890")); // Invalid characters
            Assert.ThrowsException<ArgumentException>(() => phoneNumber = new PhoneNumber("123.456.7890")); // Invalid characters
            Assert.ThrowsException<ArgumentException>(() => phoneNumber = new PhoneNumber("+1 (123) 456-7890")); // Invalid characters
        }
    }
}