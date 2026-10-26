using Domain.Model.ValueObjects;

namespace TestDomain.TestValueObjects
{
    [TestClass]
    public class TestEmail
    {
        [TestMethod]
        public void Email_InvalidValues_ShouldThrow()
        {
            Email email;
            Assert.ThrowsException<ArgumentException>(() => email = new Email(null));
            Assert.ThrowsException<ArgumentException>(() => email = new Email("   "));
            Assert.ThrowsException<ArgumentException>(() => email = new Email("invalidemail"));
            Assert.ThrowsException<ArgumentException>(() => email = new Email("invalid@.com"));
            Assert.ThrowsException<ArgumentException>(() => email = new Email("invalid@com"));
            Assert.ThrowsException<ArgumentException>(() => email = new Email("invalid@domain."));
            Assert.ThrowsException<ArgumentException>(() => email = new Email("invalid@@domain.com"));
            Assert.ThrowsException<ArgumentException>(() => email = new Email("invalid@domain.it.c"));
            //attenzione, in email >= 2 vuol dire che basta che ci sia una sola lettera perchè usiamo length e non lenth-1
        }
    }
}