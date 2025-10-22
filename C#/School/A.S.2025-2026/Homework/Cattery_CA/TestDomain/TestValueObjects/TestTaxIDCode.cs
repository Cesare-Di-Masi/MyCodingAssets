using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Model.ValueObjects;

namespace TestDomain.TestValueObjects
{
    [TestClass]
    public class TestTaxIDCode
    {

        [TestMethod]
        public void TaxIDCode_InvalidValues_ShouldThrow()
        {
            TaxIDCode taxIDCode;
            Assert.ThrowsException<ArgumentException>(() => taxIDCode = new TaxIDCode(null));
            Assert.ThrowsException<ArgumentException>(() => taxIDCode = new TaxIDCode("   "));
            Assert.ThrowsException<ArgumentException>(() => taxIDCode = new TaxIDCode("123456789")); // Too short
            Assert.ThrowsException<ArgumentException>(() => taxIDCode = new TaxIDCode("12345678901234567")); // Too long
            Assert.ThrowsException<ArgumentException>(() => taxIDCode = new TaxIDCode("1234@67890ABCD12")); // Invalid characters
            Assert.ThrowsException<ArgumentException>(() => taxIDCode = new TaxIDCode("1234 67890ABCD12")); // Invalid characters
            Assert.ThrowsException<ArgumentException>(() => taxIDCode = new TaxIDCode("1234-67890ABCD12")); // Invalid characters
        }

    }
}
