namespace BattagliaNavale.Models
{
    public class PartitaStatistiche
    {
        public int Id { get; set; }
        public Risultato RisultatoPartita { get; set; }
        public TimeSpan TempoPartita { get; set; }

        public string TempoPartitaTesto => $"{TempoPartita.Minutes}:{TempoPartita.Seconds:00}";

        public List<PosizioneBarca> BarcheBot { get; set; } = new();
        public List<PosizioneBarca> BarchePlayer { get; set; } = new();
        public List<CellaCampo> CampoBot { get; set; } = new();
        public List<CellaCampo> CampoPlayer { get; set; } = new();
    }
}
