using MastermindLib;

namespace MastermindLibTest
{
    //GameManager da copiare
    //GameManager game = new GameManager(false, 4, 4, 3, 1);
    [TestClass]
    public class CodeBreakerTest
    {
        [TestMethod]
        public void CodeBreaker_Name_IsIllegal()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { CodeBreaker breaker = new CodeBreaker("", 6); });
        }

        [TestMethod]
        public void CodeBreaker_NextColour_IsCorrect_Next()
        {
            CodeBreaker breaker = new CodeBreaker("Gionni", 6);

            Colours actual = Colours.Blue;
            Colours expected = Colours.Green;

            breaker.NextColour(ref actual);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CodeBreaker_NextColour_IsCorrect_ReturnToStart()
        {
            CodeBreaker breaker = new CodeBreaker("Gionni", 4);

            Colours actual = Colours.Yellow;
            Colours expected = Colours.Red;

            breaker.NextColour(ref actual);

            Assert.AreEqual(expected, actual);
        }
    }
}