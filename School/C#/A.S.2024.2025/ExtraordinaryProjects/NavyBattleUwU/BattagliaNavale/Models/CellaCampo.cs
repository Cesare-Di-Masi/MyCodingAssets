namespace BattagliaNavale.Models
{
    public class CellaCampo
    {
        public StatoCampo Stato { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public CellaCampo() { } 

        public CellaCampo(StatoCampo stato, int x, int y)
        {
            Stato = stato;
            X = x;
            Y = y;
        }
    }
}
