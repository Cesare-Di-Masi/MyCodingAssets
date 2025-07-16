using MastermindLib;

namespace MastermindLibTest
{
    [TestClass]
    public class CodeGeneratorTest
    {
        [TestMethod]
        public void CodeGenerator_GenerateCode_throw_Exception()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { CodeGenerator generator = new CodeGenerator(6, 4, 1); });
        }

        [TestMethod]
        public void CodeGenerator_GenerateCode_Is_Correct()
        {
            CodeGenerator generator = new CodeGenerator(6, 6, 1);

            Colours[] col = generator.generateCode();
            int[] check = new int[col.Length];
            bool cor = false;

            for (int i = 0; i < col.Length; i++)
            {
                check[(int)col[i]]++;
                if (check[(int)col[i]] > 1)
                    cor = true;
            }

            Assert.AreEqual(false, cor);
        }
    }
}