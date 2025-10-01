using Domain;
using Domain.Model.Entities;
using Domain.Model.ValueObjects;

namespace TestDomain.TestEntities
{
    internal class TestAnimalClass : Animal
    {
        public TestAnimalClass(string name, List<VeterinaryVisit>? visits = null) : base(name, visits)
        {
        }

        public TestAnimalClass(string name, Birthdate birthdate, Breed breed, List<VeterinaryVisit>? visits = null) : base(name, birthdate, breed, visits)
        {
        }
    }

    [TestClass]
    public sealed class TestAnimal
    {
        [TestMethod]
        public void Animal_InvalidValues_ShouldThrow()
        {
            //in questo test andiamo a testare i costruttori generali della classe animal, quindi breed per esempio adesso non verrà testato
            TestAnimalClass test;
            TestAnimalClass other = new TestAnimalClass("other");
            List<VeterinaryVisit> Invalidvisits = new List<VeterinaryVisit>(); //liste non valide per testare il costruttore
            Veterinary veterinary = new Veterinary("vet", "test", "test@test.it", "123456789", "specialization");

            Invalidvisits.Add(new VeterinaryVisit(other, veterinary, DateTime.Now, VisitResults.CHECK_NOTES, "nothing"));

            Assert.ThrowsException<ArgumentNullException>(() => test = new TestAnimalClass(null));
            Assert.ThrowsException<ArgumentException>(() => test = new TestAnimalClass("p", Invalidvisits));
        }

        [TestMethod]
        public void Animal_ValidValues_ShouldCreate()
        {
            TestAnimalClass test = new TestAnimalClass("bobby", new Birthdate(DateOnly.FromDateTime(DateTime.Now)), new Breed("labrador", "dog"), new List<VeterinaryVisit>());
            Assert.AreEqual("bobby", test.Name);
        }

        [TestMethod]
        public void Animal_AddVisit_ShouldAdd()
        {
            TestAnimalClass test = new TestAnimalClass("bobby", new Birthdate(DateOnly.FromDateTime(DateTime.Now)), new Breed("labrador", "dog"), new List<VeterinaryVisit>());
            Veterinary veterinary = new Veterinary("vet", "test", "test@test.it", "123456789", "specialization");
            test.AddVisit(new VeterinaryVisit(test, veterinary, DateTime.Now, VisitResults.CHECK_NOTES, "nothing"));
        }

        [TestMethod]
        public void Animal_AddVisit_Invalid_ShouldThrow()
        {
            TestAnimalClass test = new TestAnimalClass("bobby", new Birthdate(DateOnly.FromDateTime(DateTime.Now)), new Breed("labrador", "dog"), new List<VeterinaryVisit>());
            TestAnimalClass other = new TestAnimalClass("other");
            Veterinary veterinary = new Veterinary("vet", "test", "test@test.it", "123456789", "specialization");
            Assert.ThrowsException<ArgumentException>(() => test.AddVisit(new VeterinaryVisit(other, veterinary, DateTime.Now, VisitResults.CHECK_NOTES, "nothing")));
        }

        [TestMethod]
        public void Animal_FavouriteFood_ShouldSet()
        {
            TestAnimalClass test = new TestAnimalClass("bobby", new Birthdate(DateOnly.FromDateTime(DateTime.Now)), new Breed("labrador", "dog"), new List<VeterinaryVisit>());
            test.FavouriteFood = "croccantini";
            Assert.AreEqual("croccantini", test.FavouriteFood);
            test.FavouriteFood = null;
            Assert.IsNull(test.FavouriteFood);
            Assert.ThrowsException<ArgumentException>(() => test.FavouriteFood = "   ");
        }

        [TestMethod]
        public void Animal_FavouriteGame_ShouldSet()
        {
            TestAnimalClass test = new TestAnimalClass("bobby", new Birthdate(DateOnly.FromDateTime(DateTime.Now)), new Breed("labrador", "dog"), new List<VeterinaryVisit>());
            test.FavouriteGame = "palla";
            Assert.AreEqual("palla", test.FavouriteGame);
            test.FavouriteGame = null;
            Assert.IsNull(test.FavouriteGame);
            Assert.ThrowsException<ArgumentException>(() => test.FavouriteGame = "");
        }
    }
}