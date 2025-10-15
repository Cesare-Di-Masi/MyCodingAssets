using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Model.ValueObjects;

namespace TestDomain.TestValueObjects
{
    [TestClass]
    public class TestBirthdate
    {
        [TestMethod]
        public void Birthdate_InvalidValues_ShouldThrow()
        {
            Birthdate birthdate;
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => birthdate = new Birthdate(DateOnly.FromDateTime(DateTime.Now.AddYears(1))));
        }
    }
}