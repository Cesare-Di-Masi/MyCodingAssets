namespace BattagliaNavale.Models
{
    public class Player
    {
        public int Contatore { get; set; }
        public IGenerator Generator { get; set; }
        public StatoCampo[,] Campo { get; private set; }
        public Player(StatoCampo[,] campo, IGenerator? generator = null)
        {
            Campo = campo;
            Contatore = 12;

            if (generator != null)
            {
                Generator = generator;
            }
            else
            {
                Generator = new RandomGenerator();
            }
        }
        public int[] FaiMossa(int x, int y)
        {
            if (x < 0 || x >= Campo.GetLength(0))
                throw new ArgumentOutOfRangeException(nameof(x), "immetti una x valida");
            if (y < 0 || y >= Campo.GetLength(1))
                throw new ArgumentOutOfRangeException(nameof(y), "immetti una y valida");

            return new int[] { x, y };
        }
    }
}
