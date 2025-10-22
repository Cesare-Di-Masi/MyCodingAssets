using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Assert.ThrowsException<ArgumentException>(() => breed = new Breed("name", null));
            Assert.ThrowsException<ArgumentException>(() => breed = new Breed("   ", "type"));
            Assert.ThrowsException<ArgumentException>(() => breed = new Breed("name", "   "));
        }
    }
}