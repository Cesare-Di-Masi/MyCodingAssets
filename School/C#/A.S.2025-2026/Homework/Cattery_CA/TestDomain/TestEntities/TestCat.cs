using Domain.Model.Entities;

namespace TestDomain.TestEntities
{
    [TestClass]
    public class TestCat
    {
        [TestMethod]
        public void Cat_InvalidValues_ShouldThrow()
        {
            DateOnly birthdate = DateOnly.FromDateTime(DateTime.Now);
            DateOnly arrivingDate = DateOnly.FromDateTime(DateTime.Now);
            Cat cat;
            Assert.ThrowsException<ArgumentException>(() => cat = new Cat(" ", true, birthdate, arrivingDate, null, ""));
            Assert.ThrowsException<ArgumentException>(() => cat = new Cat("Bob", true, birthdate.AddDays(1), arrivingDate, null, ""));
            Assert.ThrowsException<ArgumentException>(() => cat = new Cat("Bob", true, birthdate, arrivingDate.AddDays(1), null, ""));
        }

        [TestMethod]
        public void Cat_ValidValues_ShouldCreateCat()
        {
            DateOnly birthdate = DateOnly.FromDateTime(DateTime.Now.AddDays(-100));
            DateOnly arrivingDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-50));

            Cat cat = new Cat("Whiskers", true, arrivingDate, birthdate, null, "A friendly cat.");

            Assert.AreEqual("Whiskers", cat.Name);
        }

        [TestMethod]
        public void Cat_ModifyBirthDate_InvalidValue_ShouldThrow()
        {
            DateOnly birthdate = DateOnly.FromDateTime(DateTime.Now);
            DateOnly arrivingDate = DateOnly.FromDateTime(DateTime.Now);
            Cat cat = new Cat("Whiskers", true, arrivingDate, birthdate, null, "A friendly cat.");
            Assert.ThrowsException<ArgumentException>(() => cat.ModifyBirthDate(DateOnly.FromDateTime(DateTime.Now.AddDays(1))));
        }

        [TestMethod]
        public void Cat_ModifyBirthDate_ValidValue_ShouldUpdate()
        {
            DateOnly birthdate = DateOnly.FromDateTime(DateTime.Now);
            DateOnly arrivingDate = DateOnly.FromDateTime(DateTime.Now);
            Cat cat = new Cat("Whiskers", true, arrivingDate, birthdate, null, "A friendly cat.");
            DateOnly newBirthdate = birthdate.AddDays(-10);
            cat.ModifyBirthDate(newBirthdate);
            Assert.AreEqual(newBirthdate, cat.BirthDate);
        }

        public void Cat_ModifyDescription_ShouldUpdate()
        {
            DateOnly birthdate = DateOnly.FromDateTime(DateTime.Now);
            DateOnly arrivingDate = DateOnly.FromDateTime(DateTime.Now);
            Cat cat = new Cat("Whiskers", true, arrivingDate, birthdate, null, "A friendly cat.");
            string newDescription = "A very friendly cat.";
            cat.ModifyDescription(newDescription);
            Assert.AreEqual(newDescription, cat.Description);
        }

        [TestMethod]
        public void Cat_ToString_ShouldReturnCorrectFormat()
        {
            DateOnly birthdate = DateOnly.FromDateTime(DateTime.Now.AddDays(-100));
            DateOnly arrivingDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-50));
            Cat cat = new Cat("Whiskers", true, arrivingDate, birthdate, null, "A friendly cat.");
            string expectedString = $"Whiskers is a male cat, arrived on {arrivingDate}.";

            Assert.AreEqual(expectedString, cat.ToString());
        }

        [TestMethod]
        public void GenerateId_ShouldCreate_CorrectFormat()
        {
            DateOnly birthdate = DateOnly.FromDateTime(DateTime.Now.AddDays(-100));
            DateOnly arrivingDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-50));
            Cat cat = new Cat("Whiskers", true, arrivingDate, birthdate, null, "A friendly cat.");
            string id = cat.ID;
            string substr = id.Substring(0, 3);
            Assert.IsFalse(string.IsNullOrWhiteSpace(id));
            Assert.AreEqual(13, id.Length);
            Assert.IsTrue(int.TryParse(substr, out _));
        }
    }
}