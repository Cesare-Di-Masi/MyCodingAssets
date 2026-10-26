namespace BattagliaNavale.Models
{
    public class PosizioneBarca
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool Verticale { get; set; }

        public PosizioneBarca() { } 

        public PosizioneBarca(int x, int y, bool verticale)
        {
            X = x;
            Y = y;
            Verticale = verticale;
        }
    }
}
