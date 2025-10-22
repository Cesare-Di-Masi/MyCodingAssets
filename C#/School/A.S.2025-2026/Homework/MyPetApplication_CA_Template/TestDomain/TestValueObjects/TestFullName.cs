using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Model.ValueObjects;

namespace TestDomain.TestValueObjects
{
    [TestClass]
    public class TestFullName
    {
        [TestMethod]
        public void FullName_InvalidValues_ShouldThrow()
        {
            FullName fullName;
            Assert.ThrowsException<ArgumentException>(() => fullName = new FullName(null, "lastname"));
            Assert.ThrowsException<ArgumentException>(() => fullName = new FullName("firstname", null));
            Assert.ThrowsException<ArgumentException>(() => fullName = new FullName("   ", "lastname"));
            Assert.ThrowsException<ArgumentException>(() => fullName = new FullName("firstname", "   "));
        }
    }
}