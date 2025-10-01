using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain;
using Domain.Model.Entities;

namespace TestDomain.TestEntities
{
    [TestClass]
    public class TestVeterinary
    {
        [TestMethod]
        public void Veterinary_InvalidValues_ShouldThrow()
        {
            Veterinary vet;
            Assert.ThrowsException<ArgumentException>(() => vet = new Veterinary(null, "test", "test@test.it", "123456789", "specialization"));
            Assert.ThrowsException<ArgumentException>(() => vet = new Veterinary("vet", null, "test@test.it", "123456789", "specialization"));
            Assert.ThrowsException<ArgumentException>(() => vet = new Veterinary("vet", "test", null, "123456789", "specialization"));
            Assert.ThrowsException<ArgumentException>(() => vet = new Veterinary("vet", "test", "test@test.it", null, "specialization"));
        }

        [TestMethod]
        public void Veterinary_ValidValues_ShouldCreate()
        {
            Veterinary vet = new Veterinary("vet", "test", "test@test.it", "123456789", "specialization");
            Assert.AreEqual("vet", vet.Name.First);
            Assert.AreEqual("test", vet.Name.Last);
            Assert.AreEqual("test@test.it", vet.Email.Value);
            Assert.AreEqual("123456789", vet.Phone.Value);
            Assert.AreEqual("specialization", vet.Specialization);
        }

        [TestMethod]
        public void Veterinary_ValidValues_NoSpecialization_ShouldCreateGeneral()
        {
            Veterinary vet = new Veterinary("vet", "test", "test@test.it", "123456789", null);

            Assert.AreEqual("General", vet.Specialization);
        }
    }
}