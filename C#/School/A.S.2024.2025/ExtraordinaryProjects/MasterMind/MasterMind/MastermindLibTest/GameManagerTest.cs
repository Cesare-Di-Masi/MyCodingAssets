using MastermindLib;

namespace MastermindLibTest
{
    //game manager di testo per copia incolla
    //GameManager game = new GameManager(false, 4, 4, 3, 1);
    [TestClass]
    public class GameManagerTest
    {
        [TestMethod]
        public void GameManager_codeLength_Illegal()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { GameManager game = new GameManager(false, 3, 4, 3, 1); });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { GameManager game = new GameManager(false, 13, 4, 3, 1); });
        }

        [TestMethod]
        public void GameManager_nColours_Illegal()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { GameManager game = new GameManager(false, 4, 3, 3, 1); });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { GameManager game = new GameManager(false, 4, 21, 3, 1); });
        }

        [TestMethod]
        public void GameManager_nAttempts_Illegal()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { GameManager game = new GameManager(false, 4, 4, 0, 1); });
        }

        [TestMethod]
        public void GameManager_codeComplexity_Illegal()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { GameManager game = new GameManager(false, 4, 4, 3, 0); });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { GameManager game = new GameManager(false, 4, 4, 3, 6); });
        }

        [TestMethod]
        public void GameManager_TryCode_Tips_Are_Correct_AllWrong_True()
        {
            FixedColoursGenerator generator = new FixedColoursGenerator();
            GameManager game = new GameManager(false, 4, 4, 3, 1, generator);
            Colours[] attempt = new Colours[4] { Colours.Green, Colours.Green, Colours.Green, Colours.Green };
            game.EndOfTheTurn(attempt);
            bool actual = game.IsAllWrong;
            bool expected = true;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GameManager_TryCode_Tips_Are_Correct_AllWrong_False()
        {
            FixedColoursGenerator generator = new FixedColoursGenerator();
            GameManager game = new GameManager(false, 4, 4, 3, 1, generator);
            Colours[] attempt = new Colours[4] { Colours.Blue, Colours.Blue, Colours.Red, Colours.Blue };
            game.EndOfTheTurn(attempt);
            bool actual = game.IsAllWrong;
            bool expected = false;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GameManager_TryCode_Tips_Are_Correct_CorrectColoursCorrectPos()
        {
            FixedColoursGenerator generator = new FixedColoursGenerator();
            GameManager game = new GameManager(false, 4, 4, 3, 1, generator);
            Colours[] attempt = new Colours[4] { Colours.Blue, Colours.Red, Colours.Red, Colours.Blue };
            game.EndOfTheTurn(attempt);
            int actual = game.RightPosition;
            int expected = 2;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GameManager_TryCode_Tips_Are_Correct_CorrectColoursCorrectPos_0()
        {
            FixedColoursGenerator generator = new FixedColoursGenerator();
            GameManager game = new GameManager(false, 4, 4, 3, 1, generator);
            Colours[] attempt = new Colours[4] { Colours.Red, Colours.Blue, Colours.Red, Colours.Blue };
            game.EndOfTheTurn(attempt);
            int actual = game.RightPosition;
            int expected = 0;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GameManager_TryCode_Tips_Are_Correct_WrongPosition()
        {
            FixedColoursGenerator generator = new FixedColoursGenerator();
            GameManager game = new GameManager(false, 4, 4, 3, 1, generator);
            Colours[] attempt = new Colours[4] { Colours.Red, Colours.Blue, Colours.Red, Colours.Blue };
            game.EndOfTheTurn(attempt);
            int actual = game.WrongPosition;
            int expected = 4;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GameManager_PlayerIsOn_SolutionCode_And_GuessIsCorrect()
        {
            Colours[] playerCode = new Colours[4] { Colours.Red, Colours.Blue, Colours.Green, Colours.Blue };
            Colours[] solutionCode = new Colours[4] { Colours.Red, Colours.Blue, Colours.Green, Colours.Blue };

            GameManager game = new GameManager(solutionCode, false, 4, 4, 3, 1);

            GameStatus correct = game.EndOfTheTurn(playerCode);
            GameStatus expected = GameStatus.Won;

            Assert.AreEqual(expected, correct);
        }

        [TestMethod]
        public void GameManager_BotIsOn_SolutionCode_And_GuessIsCorrect()
        {
            FixedColoursGenerator generator = new FixedColoursGenerator();

            GameManager game = new GameManager(false, 4, 4, 3, 1, generator);

            Colours[] sol = new Colours[4] { Colours.Blue, Colours.Red, Colours.Blue, Colours.Red };

            GameStatus correct = game.EndOfTheTurn(sol);
            GameStatus expected = GameStatus.Won;

            Assert.AreEqual(expected, correct);
        }

        public void GameManager_BotIsOn_WrongGuess()
        {
            FixedColoursGenerator generator = new FixedColoursGenerator();

            GameManager game = new GameManager(false, 4, 4, 3, 1, generator);

            Colours[] sol = new Colours[4] { Colours.Red, Colours.Red, Colours.Red, Colours.Blue };

            GameStatus correct = game.EndOfTheTurn(sol);
            GameStatus expected = GameStatus.Playing;

            Assert.AreEqual(expected, correct);
        }

        public void GameManager_BotIsOn_WrongGuess_LostGame()
        {
            FixedColoursGenerator generator = new FixedColoursGenerator();

            GameManager game = new GameManager(false, 4, 4, 1, 1, generator);

            Colours[] sol = new Colours[4] { Colours.Red, Colours.Red, Colours.Red, Colours.Blue };

            GameStatus correct = game.EndOfTheTurn(sol);
            GameStatus expected = GameStatus.Lost;

            Assert.AreEqual(expected, correct);
        }
    }
}