using Domain.Model.ValueObjects;

namespace TestDomain.TestValueObjects
{
    [TestClass]
    public class TestBreed
    {
        [TestMethod]
        public void Breed_InvalidValues_ShouldThrow()
        {
            Breed breed;
            Assert.ThrowsException<ArgumentException>(() => breed = new Breed(null, "type"));
            Assert.ThrowsException<ArgumentException>(() => breed = new Breed("   ", "type"));
        }

        [TestMethod]
        public void Breed_ValidValues_ShouldCreateBreed()
        {
            Breed breed = new Breed("Siamese", "Cat");
            Assert.AreEqual("Siamese", breed.Name);
            Assert.AreEqual("Cat", breed.Description);
        }

        [TestMethod]
        public void Breed_DescriptionIsNull_ShouldCreateBreed()
        {
            Breed breed = new Breed("Siamese", null);
            Assert.AreEqual("Siamese", breed.Name);
            Assert.AreEqual("no description", breed.Description);
        }
    }
}