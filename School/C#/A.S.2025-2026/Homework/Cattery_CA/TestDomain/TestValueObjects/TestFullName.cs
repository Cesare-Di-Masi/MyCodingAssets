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