using Domain.Model.Entities;
using Domain.Model.ValueObjects;

namespace TestDomain.TestEntities
{
    [TestClass]
    public class TestAdoption
    {
        private Cat validCat = new Cat("Whiskers", true, DateOnly.FromDateTime(DateTime.Now.AddDays(-50)), DateOnly.FromDateTime(DateTime.Now.AddDays(-100)), null, "A friendly cat.");

        private Adopter validAdopter = new Adopter
            (new FullName("Mario", "Rossi"), DateOnly.FromDateTime(DateTime.Now.AddYears(-25)), new TaxIDCode("RSSMRA85RTY7IPD3"), new CAP("00100")
            , new Email("mario.ross@gmail.com)"));

        [TestMethod]
        public void Adoption_InvalidValues_ShouldThrow()
        {
            Assert.ThrowsException<ArgumentException>(() => new Adoption(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), validCat, validAdopter, null));
        }

        [TestMethod]
        public void Adoption_ValidValues_ShouldCreateAdoption()
        {
            DateOnly adoptionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
            Adoption adoption = new Adoption(adoptionDate, validCat, validAdopter, "A lovely adoption.");
            Assert.AreEqual(adoptionDate, adoption.AdoptionDate);
            Assert.AreEqual(validCat, adoption.Cat);
            Assert.AreEqual(validAdopter, adoption.Adopter);
            Assert.AreEqual("A lovely adoption.", adoption.Description);
        }

        [TestMethod]
        public void Adoption_ModifyDescription_ShouldUpdate()
        {
            DateOnly adoptionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
            Adoption adoption = new Adoption(adoptionDate, validCat, validAdopter, "A lovely adoption.");
            string newDescription = "An even lovelier adoption.";
            adoption.ModifyDescription(newDescription);
            Assert.AreEqual(newDescription, adoption.Description);
        }
    }
}