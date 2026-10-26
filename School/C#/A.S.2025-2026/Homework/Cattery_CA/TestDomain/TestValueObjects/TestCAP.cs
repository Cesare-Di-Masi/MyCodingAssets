using Domain.Model.ValueObjects;

namespace TestDomain.TestValueObjects
{
    [TestClass]
    public class TestCAP
    {
        [TestMethod]
        public void CAP_invalidValues_ShouldThrow()
        {
            List<string> invalidValues = new List<string>
            {
                "",
                "1234",
                "123456",
                "12a45",
                "ABCDE",
                "123 5",
                "12-45"
            };
            foreach (var value in invalidValues)
            {
                Assert.ThrowsException<ArgumentException>(() => new CAP(value), $"Expected exception for value: '{value}'");
            }
        }
    }
}