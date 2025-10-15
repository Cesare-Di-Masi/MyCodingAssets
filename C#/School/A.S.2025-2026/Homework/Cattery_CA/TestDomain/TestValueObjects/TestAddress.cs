using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Model.ValueObjects;

namespace TestDomain.TestValueObjects
{
    [TestClass]
    public class TestAddress
    {
        [TestMethod]
        public void Address_InvalidValues_ShouldThrow()
        {
            Address address;
            Assert.ThrowsException<ArgumentException>(() => address = new Address(null, "city", "12345"));
            Assert.ThrowsException<ArgumentException>(() => address = new Address("street", null, "12345"));
            Assert.ThrowsException<ArgumentException>(() => address = new Address("   ", "city", "12345"));
            Assert.ThrowsException<ArgumentException>(() => address = new Address("street", "   ", "12345"));
        }
    }
}