using SeasonExtensionMethodLib;

namespace TestSeasonExtMethod
{
    [TestClass]
    public class TestSeasonExtMethods
    {
        [TestMethod]
        public void GetSeason_IsCorrect()
        {
            DateTime spring = new DateTime(2000, 3, 22);
            DateTime summer = new DateTime(2000, 6, 22);
            DateTime fall = new DateTime(2000, 9, 23);
            DateTime winter = new DateTime(2000, 12, 22);

            Assert.AreEqual(Seasons.SPRING, spring.GetSeason());
            Assert.AreEqual(Seasons.SUMMER, summer.GetSeason());
            Assert.AreEqual(Seasons.FALL, fall.GetSeason());
            Assert.AreEqual(Seasons.WINTER, winter.GetSeason());
        }

        [TestMethod]
        public void IsSummer_IsCorrect()
        {
            DateTime summer = new DateTime(2000, 6, 21);
            DateTime notSummer = new DateTime(2000, 3, 20);
            Assert.IsTrue(summer.IsSummer());
            Assert.IsFalse(notSummer.IsSummer());
        }

        [TestMethod]
        public void DaysForNextSeason_IsCorrect()
        {
            DateTime date = new DateTime(2000, 1, 1);
            Assert.AreEqual(79, date.DaysForNextSeason());
        }
    }
}