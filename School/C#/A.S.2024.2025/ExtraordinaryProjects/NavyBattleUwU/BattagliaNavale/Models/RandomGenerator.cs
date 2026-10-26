namespace BattagliaNavale.Models
{
    internal class RandomGenerator : IGenerator
    {
        private Random random = new Random();

        public int GeneraMossaX(int maxX)
        {
            return random.Next(0, maxX);
        }

        public int GeneraMossaY(int maxY)
        {
            return random.Next(0, maxY);
        }

        public bool GeneraOrientamentoBarca()
        {
            int booleano = random.Next(0, 2);
            if (booleano == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public int GeneraOrientamentoMossa()
        {
            return random.Next(0, 4);
        }
    }
}
