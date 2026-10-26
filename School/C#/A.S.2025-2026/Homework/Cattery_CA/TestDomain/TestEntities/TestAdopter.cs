using Domain.Model.Entities;
using Domain.Model.ValueObjects;

namespace TestDomain.TestEntities
{
    [TestClass]
    public class TestAdopter
    {
        private FullName validName = new FullName("Mario", "Rossi");
        private TaxIDCode validTaxID = new TaxIDCode("RSSMRA85RTY7IPD3");
        private CAP validCAP = new CAP("00100");
        private DateOnly validBirthDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-25));
        private PhoneNumber validPhoneNumber = new PhoneNumber("1234567890");
        private Email validMail = new Email("Mario.Rossi@gmail.com");

        [TestMethod]
        public void Adopter_InvalidValues_ShouldThrow()
        {
            Assert.ThrowsException<ArgumentException>(() => new Adopter(validName, validBirthDate.AddYears(26), validTaxID, validCAP, validMail, validPhoneNumber));
        }

        [TestMethod]
        public void Adopter_ValidValues_ShouldCreateAdopter()
        {
            Adopter adopter = new Adopter(validName, validBirthDate, validTaxID, validCAP, validMail, validPhoneNumber);
            Assert.AreEqual(validName, adopter.FullName);
            Assert.AreEqual(validBirthDate, adopter.BirthDate);
            Assert.AreEqual(validMail, adopter.Email);
            Assert.AreEqual(validPhoneNumber, adopter.PhoneNumber);
        }

        [TestMethod]
        public void Adopter_WithoutPhoneNumber_ShouldCreateAdopter()
        {
            Adopter adopter = new Adopter(validName, validBirthDate, validTaxID, validCAP, validMail);
            Assert.AreEqual(validName, adopter.FullName);
            Assert.AreEqual(validBirthDate, adopter.BirthDate);
            Assert.AreEqual(validMail, adopter.Email);
            Assert.IsNull(adopter.PhoneNumber);
        }
    }
}