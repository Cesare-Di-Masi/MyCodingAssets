namespace BattagliaNavale.Models
{
    public interface IGenerator
    {
        public int GeneraMossaX(int maxX);

        public int GeneraMossaY(int maxY);

        public bool GeneraOrientamentoBarca();

        public int GeneraOrientamentoMossa();
    }
}
