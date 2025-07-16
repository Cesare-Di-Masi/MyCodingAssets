using BattagliaNavale.Models;
using BattagliaNavale.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BattagliaNavale.ViewModels
{
    public partial class PosizionaNaviViewModel : ObservableObject
    {
        private const int GrigliaDimensione = 10;
        private readonly INavigation _navigation;

        [ObservableProperty]
        private int naveCorrente = 0;

        public List<int> NaviDisponibili { get; } = new() { 2, 3, 3, 4 };

        // Posizioni e orientamenti temporanei delle navi
        private List<Tuple<int, int, bool>> NaviPosizionate = new()
        {
            new(0, 0, false),
            new(0, 1, false),
            new(0, 2, false),
            new(0, 3, false),
        };

        public int PrimaNaveRow => NaviPosizionate[0].Item1;
        public int PrimaNaveColumn => NaviPosizionate[0].Item2;
        public double PrimaNaveRotation { get; set; } = 0;
        public int PrimaNaveRowSpan { get; set; } = 2;
        public int PrimaNaveColumnSpan { get; set; } = 1;
        public double PrimaNaveScale { get; set; } = 1.0;

        public int SecondaNaveRow => NaviPosizionate[1].Item1;
        public int SecondaNaveColumn => NaviPosizionate[1].Item2;
        public double SecondaNaveRotation { get; set; } = 0;
        public int SecondaNaveRowSpan { get; set; } = 3;
        public int SecondaNaveColumnSpan { get; set; } = 1;
        public double SecondaNaveScale { get; set; } = 1.0;

        public int TerzaNaveRow => NaviPosizionate[2].Item1;
        public int TerzaNaveColumn => NaviPosizionate[2].Item2;
        public double TerzaNaveRotation { get; set; } = 0;
        public int TerzaNaveRowSpan { get; set; } = 3;
        public int TerzaNaveColumnSpan { get; set; } = 1;
        public double TerzaNaveScale { get; set; } = 1.0;

        public int QuartaNaveRow => NaviPosizionate[3].Item1;
        public int QuartaNaveColumn => NaviPosizionate[3].Item2;
        public double QuartaNaveRotation { get; set; } = 0;
        public int QuartaNaveRowSpan { get; set; } = 4;
        public int QuartaNaveColumnSpan { get; set; } = 1;
        public double QuartaNaveScale { get; set; } = 1.0;

        private StatoCampo[,] campoLogico;

        public PosizionaNaviViewModel(INavigation navigation)
        {
            campoLogico = new StatoCampo[GrigliaDimensione, GrigliaDimensione];

            _navigation = navigation;
            AggiornaGrigliaTemporanea();
        }

        [RelayCommand]
        private async Task ConfermaPosizionamentoAsync()
        {
            await _navigation.PushAsync(new Gioco(campoLogico, NaviPosizionate));
        }

        [RelayCommand]
        private void SelezionaNave(int index)
        {
            NaveCorrente = index;
            NotificaPosizioniNavi();
            AggiornaGrigliaTemporanea();
        }

        [RelayCommand]
        private void RuotaNave()
        {
            var (x, y, orientamentoOrizzontale) = NaviPosizionate[NaveCorrente];

            if (PuoPosizionarsi(x, y, NaviDisponibili[NaveCorrente], !orientamentoOrizzontale))
            {
                NaviPosizionate[NaveCorrente] = new Tuple<int, int, bool>(x, y, !orientamentoOrizzontale);
                if (!orientamentoOrizzontale)
                {
                    switch (NaveCorrente)
                    {
                        case 0:
                            PrimaNaveRotation = 90;
                            PrimaNaveRowSpan = 1;
                            PrimaNaveColumnSpan = NaviDisponibili[0];
                            PrimaNaveScale = 2.0;
                            break;
                        case 1:
                            SecondaNaveRotation = 90;
                            SecondaNaveRowSpan = 1;
                            SecondaNaveColumnSpan = NaviDisponibili[1];
                            SecondaNaveScale = 3;
                            break;
                        case 2:
                            TerzaNaveRotation = 90;
                            TerzaNaveRowSpan = 1;
                            TerzaNaveColumnSpan = NaviDisponibili[2];
                            TerzaNaveScale = 2.7;
                            break;
                        case 3:
                            QuartaNaveRotation = 90;
                            QuartaNaveRowSpan = 1;
                            QuartaNaveColumnSpan = NaviDisponibili[3];
                            QuartaNaveScale = 3.7;
                            break;
                    }
                }
                else
                {
                    switch (NaveCorrente)
                    {
                        case 0:
                            PrimaNaveRotation = 0;
                            PrimaNaveRowSpan = NaviDisponibili[0];
                            PrimaNaveColumnSpan = 1;
                            PrimaNaveScale = 1.0;
                            break;
                        case 1:
                            SecondaNaveRotation = 0;
                            SecondaNaveRowSpan = NaviDisponibili[1];
                            SecondaNaveColumnSpan = 1;
                            SecondaNaveScale = 1.0;
                            break;
                        case 2:
                            TerzaNaveRotation = 0;
                            TerzaNaveRowSpan = NaviDisponibili[2];
                            TerzaNaveColumnSpan = 1;
                            TerzaNaveScale = 1.0;
                            break;
                        case 3:
                            QuartaNaveRotation = 0;
                            QuartaNaveRowSpan = NaviDisponibili[3];
                            QuartaNaveColumnSpan = 1;
                            QuartaNaveScale = 1.0;
                            break;
                    }
                }
                NotificaPosizioniNavi();
                AggiornaGrigliaTemporanea();
            }
        }


        [RelayCommand]
        private void SpostaNave(Point direzione)
        {
            var (x, y, orientamentoOrizzontale) = NaviPosizionate[NaveCorrente];
            int nuovaX = x + (int)direzione.X;
            int nuovaY = y + (int)direzione.Y;

            if (PuoPosizionarsi(nuovaX, nuovaY, NaviDisponibili[NaveCorrente], orientamentoOrizzontale))
            {
                NaviPosizionate[NaveCorrente] = new Tuple<int, int, bool>(nuovaX, nuovaY, orientamentoOrizzontale);
                NotificaPosizioniNavi();
                AggiornaGrigliaTemporanea();
            }
        }

        private void AggiornaGrigliaTemporanea()
        {
            for (int i = 0; i < GrigliaDimensione; i++)
            {
                for (int j = 0; j < GrigliaDimensione; j++)
                {
                    campoLogico[i, j] = StatoCampo.ACQUA;
                }
            }

            for (int i = 0; i < NaviPosizionate.Count; i++)
            {
                var (x, y, orientamentoOrizzontale) = NaviPosizionate[i];
                int lunghezza = NaviDisponibili[i];
                for (int j = 0; j < lunghezza; j++)
                {
                    int posX = orientamentoOrizzontale ? x : x + j;
                    int posY = orientamentoOrizzontale ? y + j : y;
                    if (posX < GrigliaDimensione && posY < GrigliaDimensione)
                    {
                        campoLogico[posX, posY] = StatoCampo.NAVE;
                    }
                }
            }
        }

        private bool PuoPosizionarsi(int x, int y, int lunghezza, bool orientamentoOrizzontale)
        {
            // Controllo che la nave stia dentro la griglia
            if (orientamentoOrizzontale)
            {
                if (y < 0 || y + lunghezza > GrigliaDimensione || x < 0 || x >= GrigliaDimensione)
                    return false;
            }
            else
            {
                if (x < 0 || x + lunghezza > GrigliaDimensione || y < 0 || y >= GrigliaDimensione)
                    return false;
            }

            // Controllo sovrapposizione con altre navi (escludendo la nave corrente)
            for (int i = 0; i < lunghezza; i++)
            {
                int posX = orientamentoOrizzontale ? x : x + i;
                int posY = orientamentoOrizzontale ? y + i : y;

                // Trova l'indice della nave corrente
                for (int n = 0; n < NaviPosizionate.Count; n++)
                {
                    if (n == NaveCorrente) continue;
                    var (nx, ny, norientamentoOrizzontale) = NaviPosizionate[n];
                    int nlunghezza = NaviDisponibili[n];
                    for (int j = 0; j < nlunghezza; j++)
                    {
                        int nposX = norientamentoOrizzontale ? nx : nx + j;
                        int nposY = norientamentoOrizzontale ? ny + j : ny;
                        if (posX == nposX && posY == nposY)
                            return false;
                    }
                }
            }

            return true;
        }



        private void NotificaPosizioniNavi()
        {
            OnPropertyChanged(nameof(PrimaNaveRow));
            OnPropertyChanged(nameof(PrimaNaveColumn));
            OnPropertyChanged(nameof(PrimaNaveRotation));
            OnPropertyChanged(nameof(PrimaNaveRowSpan));
            OnPropertyChanged(nameof(PrimaNaveColumnSpan));
            OnPropertyChanged(nameof(PrimaNaveScale));

            OnPropertyChanged(nameof(SecondaNaveRow));
            OnPropertyChanged(nameof(SecondaNaveColumn));
            OnPropertyChanged(nameof(SecondaNaveRotation));
            OnPropertyChanged(nameof(SecondaNaveRowSpan));
            OnPropertyChanged(nameof(SecondaNaveColumnSpan));
            OnPropertyChanged(nameof(SecondaNaveScale));

            OnPropertyChanged(nameof(TerzaNaveRow));
            OnPropertyChanged(nameof(TerzaNaveColumn));
            OnPropertyChanged(nameof(TerzaNaveRotation));
            OnPropertyChanged(nameof(TerzaNaveRowSpan));
            OnPropertyChanged(nameof(TerzaNaveColumnSpan));
            OnPropertyChanged(nameof(TerzaNaveScale));

            OnPropertyChanged(nameof(QuartaNaveRow));
            OnPropertyChanged(nameof(QuartaNaveColumn));
            OnPropertyChanged(nameof(QuartaNaveRotation));
            OnPropertyChanged(nameof(QuartaNaveRowSpan));
            OnPropertyChanged(nameof(QuartaNaveColumnSpan));
            OnPropertyChanged(nameof(QuartaNaveScale));
        }

    }
}