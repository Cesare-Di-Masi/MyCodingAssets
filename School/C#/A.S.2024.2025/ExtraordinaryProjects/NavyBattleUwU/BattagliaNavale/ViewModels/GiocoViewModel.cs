using BattagliaNavale.Infrastucture;
using BattagliaNavale.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace BattagliaNavale.ViewModels
{
    public partial class GiocoViewModel : ObservableObject
    {
        public ObservableCollection<ObservableCollection<StatoCampo>> CampoBot { get; private set; }
        public ObservableCollection<ObservableCollection<StatoCampo>> CampoGiocatore { get; private set; }

        public ObservableCollection<(int x, int y, bool colpito)> ColpiPlayer { get; } = new();
        public ObservableCollection<(int x, int y, bool colpito)> ColpiBot { get; } = new();

        public GameManager gameManager;

        [ObservableProperty]
        private int? selezioneX = 0;

        [ObservableProperty]
        private int? selezioneY = 0;

        [ObservableProperty]
        private Risultato messaggioRisultato;

        [ObservableProperty]
        private int ultimaBarcaAffondata = -1;

        public List<Tuple<int, int, bool>> BarchePlayer { get; } = new();

        public Stopwatch stopwatch = new Stopwatch();

        [ObservableProperty]
        private string stopwatchText = "00:00:00";

        private System.Timers.Timer timer;

        public GiocoViewModel(StatoCampo[,] campoGiocatore, List<Tuple<int, int, bool>> barchePlayer)
        {
            var player = new Player(campoGiocatore);
            var bot = new Bot(new StatoCampo[10, 10]);
            gameManager = new GameManager(player, bot);

            CampoBot = ConvertiCampo(bot.Campo);
            CampoGiocatore = ConvertiCampo(player.Campo);

            this.BarchePlayer = barchePlayer;

            stopwatch.Start();

            timer = new System.Timers.Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(() =>
            {
                StopwatchText = stopwatch.Elapsed.ToString(@"mm\:ss");
            });
        }

        private ObservableCollection<ObservableCollection<StatoCampo>> ConvertiCampo(StatoCampo[,] campo)
        {
            var grid = new ObservableCollection<ObservableCollection<StatoCampo>>();
            for (int i = 0; i < campo.GetLength(0); i++)
            {
                var row = new ObservableCollection<StatoCampo>();
                for (int j = 0; j < campo.GetLength(1); j++)
                    row.Add(campo[i, j]);
                grid.Add(row);
            }
            return grid;
        }

        [RelayCommand]
        public void SelezionaCella(Tuple<int, int> cella)
        {
            SelezioneX = cella.Item1;
            SelezioneY = cella.Item2;
        }

        [RelayCommand]
        private async Task ConfermaColpoAsync()
        {
            foreach (var colpo in ColpiPlayer)
            {
                if (colpo.x == SelezioneX.Value && colpo.y == SelezioneY.Value)
                    return;
            }

            var risultato = gameManager.VerificaVincitore(SelezioneX.Value, SelezioneY.Value);

            var statoPlayer = gameManager.Bot.Campo[SelezioneX.Value, SelezioneY.Value];
            bool colpitoPlayer = statoPlayer == StatoCampo.NAVE_COLPITA;
            ColpiPlayer.Add((SelezioneX.Value, SelezioneY.Value, colpitoPlayer));

            var mossaBot = risultato.Item2;
            var statoBot = gameManager.Giocatore.Campo[mossaBot[0], mossaBot[1]];
            bool colpitoBot = statoBot == StatoCampo.NAVE_COLPITA;
            ColpiBot.Add((mossaBot[0], mossaBot[1], colpitoBot));

            messaggioRisultato = risultato.Item1;
            ultimaBarcaAffondata = risultato.Item3;

            CheckVincitore();
            AggiornaGriglie();

            OnPropertyChanged(nameof(ColpiPlayer));
            OnPropertyChanged(nameof(ColpiBot));

            SelezioneX = 0;
            SelezioneY = 0;
        }

        private void AggiornaGriglie()
        {
            CampoBot = ConvertiCampo(gameManager.Bot.Campo);
            CampoGiocatore = ConvertiCampo(gameManager.Giocatore.Campo);
            OnPropertyChanged(nameof(CampoBot));
            OnPropertyChanged(nameof(CampoGiocatore));
        }

        private void CheckVincitore()
        {
            if (messaggioRisultato != Risultato.SOSPESO)
            {
                stopwatch.Stop();
                timer?.Stop();

                PreferencesUtilities.SaveField(
                    messaggioRisultato,
                    stopwatch.Elapsed,
                    gameManager.Bot.BarchePosizione,
                    BarchePlayer,
                    gameManager.Giocatore.Campo,
                    gameManager.Bot.Campo);
            }
        }

        public void Cleanup()
        {
            timer?.Stop();
            timer?.Dispose();
            stopwatch?.Stop();
        }
    }
}