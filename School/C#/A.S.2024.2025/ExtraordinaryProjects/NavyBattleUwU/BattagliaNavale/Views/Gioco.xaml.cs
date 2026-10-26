using BattagliaNavale.Models;
using BattagliaNavale.Services;
using BattagliaNavale.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace BattagliaNavale.Views
{
    public partial class Gioco : ContentPage
    {
        const int LUNGHEZZA = 10;

        public Gioco(StatoCampo[,] campoLogico, List<Tuple<int, int, bool>> barchePosizione)
        {
            InitializeComponent();
            BindingContext = new GiocoViewModel(campoLogico, barchePosizione);

            CreaGriglie();
            PosizionaBarche(barchePosizione);
        }

        protected override void OnDisappearing()
        {
            if (BindingContext is GiocoViewModel vm)
            {
                vm.Cleanup();
            }
            base.OnDisappearing();
        }

        private void CreaGriglie()
        {
            // Crea righe e colonne per le griglie
            for (int i = 0; i < LUNGHEZZA; i++)
            {
                grigliaBot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grigliaBot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                grigliaGiocatore.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grigliaGiocatore.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Crea bottoni per la griglia del bot
            for (int i = 0; i < LUNGHEZZA; i++)
            {
                for (int j = 0; j < LUNGHEZZA; j++)
                {
                    var bottone = new Button
                    {
                        BorderWidth = 0.5,
                        BackgroundColor = Colors.LightBlue,
                        BorderColor = Colors.CadetBlue,
                        CornerRadius = 0,
                        CommandParameter = new Tuple<int, int>(i, j)
                    };

                    bottone.Command = ((GiocoViewModel)BindingContext).SelezionaCellaCommand;

                    Grid.SetRow(bottone, i);
                    Grid.SetColumn(bottone, j);
                    grigliaBot.Children.Add(bottone);

                    var cella = new Microsoft.Maui.Controls.Shapes.Rectangle
                    {
                        Fill = new SolidColorBrush(Colors.LightBlue),
                        Stroke = new SolidColorBrush(Colors.CadetBlue),
                        StrokeThickness = 0.5
                    };
                    Grid.SetRow(cella, i);
                    Grid.SetColumn(cella, j);
                    grigliaGiocatore.Children.Add(cella);
                }
            }
        }

        private void PosizionaBarche(List<Tuple<int, int, bool>> barchePosizione)
        {
            for (int i = 0; i < barchePosizione.Count; i++)
            {
                int lunghezza = 2;
                string source = "";
                double scale = 1.0;
                switch (i)
                {
                    case 0:
                        lunghezza = 2;
                        source = "../Resources/Images/barca3.png";
                        scale = 2.0;
                        break;
                    case 1:
                        lunghezza = 3;
                        source = "../Resources/Images/barca1.png";
                        scale = 3.0;
                        break;
                    case 2:
                        lunghezza = 3;
                        source = "../Resources/Images/barca4.png";
                        scale = 2.7;
                        break;
                    case 3:
                        lunghezza = 4;
                        source = "../Resources/Images/barca2.png";
                        scale = 3.7;
                        break;
                }

                var barca = new Image
                {
                    Source = ImageSource.FromFile(source),
                    ZIndex = 1,
                    Rotation = barchePosizione[i].Item3 ? 90 : 0,
                    Scale = barchePosizione[i].Item3 ? scale : 1.0
                };

                Grid.SetRow(barca, barchePosizione[i].Item1);
                Grid.SetColumn(barca, barchePosizione[i].Item2);
                if (barchePosizione[i].Item3)
                    Grid.SetColumnSpan(barca, lunghezza);
                else
                    Grid.SetRowSpan(barca, lunghezza);

                grigliaGiocatore.Children.Add(barca);
            }
        }

        private void MostraBarcaAffondata(int indiceBarca)
        {
            var vm = (GiocoViewModel)BindingContext;
            var barcaPosizione = vm.gameManager.Bot.BarchePosizione[indiceBarca];

            int lunghezza = 2;
            string source = "";
            double scale = 1.0;

            switch (indiceBarca)
            {
                case 0:
                    lunghezza = 2;
                    source = "../Resources/Images/barca3.png";
                    scale = 2.0;
                    break;
                case 1:
                    lunghezza = 3;
                    source = "../Resources/Images/barca1.png";
                    scale = 3.0;
                    break;
                case 2:
                    lunghezza = 3;
                    source = "../Resources/Images/barca4.png";
                    scale = 2.7;
                    break;
                case 3:
                    lunghezza = 4;
                    source = "../Resources/Images/barca2.png";
                    scale = 3.7;
                    break;
            }

            var barcaAffondata = new Image
            {
                Source = ImageSource.FromFile(source),
                ZIndex = 5,
                Opacity = 0.8,
                Rotation = barcaPosizione.Item3 ? 90 : 0,
                Scale = barcaPosizione.Item3 ? scale : 1.0
            };

            Grid.SetRow(barcaAffondata, barcaPosizione.Item1);
            Grid.SetColumn(barcaAffondata, barcaPosizione.Item2);

            if (barcaPosizione.Item3)
                Grid.SetColumnSpan(barcaAffondata, lunghezza);
            else
                Grid.SetRowSpan(barcaAffondata, lunghezza);

            grigliaBot.Children.Add(barcaAffondata);
        }

        private void OnButtonClicked(object sender, EventArgs e)
        {
            var vm = (GiocoViewModel)BindingContext;

            if (vm.ColpiPlayer.Count == 0 || vm.ColpiBot.Count == 0)
            {
                return;
            }

            var colpoPlayer = vm.ColpiPlayer.LastOrDefault();

            MostraFeedbackColpo(grigliaBot, colpoPlayer.x, colpoPlayer.y, colpoPlayer.colpito);

            DisabilitaBottone(colpoPlayer.x, colpoPlayer.y);

            if (vm.UltimaBarcaAffondata != -1)
            {
                MostraBarcaAffondata(vm.UltimaBarcaAffondata);
            }

            var colpoBot = vm.ColpiBot.LastOrDefault();

            MostraFeedbackColpo(grigliaGiocatore, colpoBot.x, colpoBot.y, colpoBot.colpito);

            CheckRisultato(vm.MessaggioRisultato);
        }

        private void MostraFeedbackColpo(Grid griglia, int riga, int colonna, bool colpito)
        {

            var imgFeedback = new Image
            {
                Source = colpito
                    ? ImageSource.FromFile("../Resources/Images/fire.gif")
                    : ImageSource.FromFile("../Resources/Images/croce_img.png"),
                ZIndex = 10,
                IsAnimationPlaying = colpito,
                WidthRequest = 30,
                HeightRequest = 30,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            Grid.SetRow(imgFeedback, riga);
            Grid.SetColumn(imgFeedback, colonna);
            griglia.Children.Add(imgFeedback);


        }

        private void DisabilitaBottone(int riga, int colonna)
        {
            foreach (var child in grigliaBot.Children)
            {
                if (child is Button button &&
                    Grid.GetRow(button) == riga &&
                    Grid.GetColumn(button) == colonna)
                {
                    button.IsEnabled = false;
                    break;
                }
            }
        }

        private void CheckRisultato(Risultato risultato)
        {
            if (risultato == Risultato.PLAYER)
            {
                ShowVictoryOverlay();
            }
            else if (risultato == Risultato.BOT)
            {
                ShowDefeatOverlay();
            }
        }

        [RelayCommand]
        protected override void OnAppearing()
        {
            AudioPlayerService.Instance.Stop();
            AudioPlayerService.Instance.Play("jack-sparrow_dFQte8NI.mp3");
        }

        [RelayCommand]
        public async Task NuovoGioco()
        {
            await Navigation.PopAsync();
        }

        [RelayCommand]
        public async Task Esci()
        {
            await Navigation.PopToRootAsync();
        }


        // LOGICA PER VINCITORE


        private Grid overlayGrid;
        private bool isOverlayVisible = false;

        public async void ShowVictoryOverlay()
        {
            if (isOverlayVisible) return;

            CreateOverlay(true);
            await ShowOverlayWithAnimation();
        }

        public async void ShowDefeatOverlay()
        {
            if (isOverlayVisible) return;

            CreateOverlay(false);
            await ShowOverlayWithAnimation();
        }

        private void CreateOverlay(bool isVictory)
        {
            overlayGrid = new Grid
            {
                BackgroundColor = Color.FromArgb("#80000000"),
                IsVisible = false,
                Opacity = 0
            };

            var contentFrame = new Frame
            {
                BackgroundColor = isVictory ? Color.FromArgb("#1a237e") : Color.FromArgb("#b71c1c"),
                BorderColor = isVictory ? Colors.Gold : Colors.Red,
                CornerRadius = 20,
                HasShadow = true,
                Padding = 30,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                MaximumWidthRequest = 350
            };

            var stackLayout = new StackLayout
            {
                Spacing = 20,
                HorizontalOptions = LayoutOptions.Center
            };

            var iconLabel = new Label
            {
                Text = isVictory ? "🏆" : "💥",
                FontSize = 60,
                HorizontalOptions = LayoutOptions.Center
            };

            var titleLabel = new Label
            {
                Text = isVictory ? "VITTORIA!" : "SCONFITTA!",
                FontSize = 36,
                FontAttributes = FontAttributes.Bold,
                TextColor = isVictory ? Colors.Gold : Colors.White,
                HorizontalOptions = LayoutOptions.Center
            };

            var messageLabel = new Label
            {
                Text = isVictory ? "Hai affondato tutta la flotta nemica!" : "La tua flotta è stata distrutta!",
                FontSize = 16,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };

            var buttonStackLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 15
            };

            var newGameButton = new Button
            {
                Text = "Nuova Partita",
                BackgroundColor = isVictory ? Colors.Green : Colors.Orange,
                TextColor = Colors.White,
                CornerRadius = 10,
                FontAttributes = FontAttributes.Bold
            };
            newGameButton.Clicked += OnNewGameClicked;

            var closeButton = new Button
            {
                Text = "Chiudi",
                BackgroundColor = Colors.Gray,
                TextColor = Colors.White,
                CornerRadius = 10
            };
            closeButton.Clicked += OnCloseOverlayClicked;

            buttonStackLayout.Children.Add(newGameButton);
            buttonStackLayout.Children.Add(closeButton);

            stackLayout.Children.Add(iconLabel);
            stackLayout.Children.Add(titleLabel);
            stackLayout.Children.Add(messageLabel);
            stackLayout.Children.Add(buttonStackLayout);

            contentFrame.Content = stackLayout;
            overlayGrid.Children.Add(contentFrame);

            if (MainGrid != null)
            {
                MainGrid.Children.Add(overlayGrid);
            }
        }

        private async Task ShowOverlayWithAnimation()
        {
            isOverlayVisible = true;
            overlayGrid.IsVisible = true;

            await overlayGrid.FadeTo(1, 300, Easing.CubicOut);

            var contentFrame = overlayGrid.Children[0] as Frame;
            if (contentFrame != null)
            {
                contentFrame.Scale = 0.8;
                await contentFrame.ScaleTo(1, 200, Easing.BounceOut);
            }
        }

        private async void OnCloseOverlayClicked(object sender, EventArgs e)
        {
            await HideOverlay();

            Esci();
        }

        private async void OnNewGameClicked(object sender, EventArgs e)
        {
            await HideOverlay();

            NuovoGioco();
        }

        private async Task HideOverlay()
        {
            if (!isOverlayVisible || overlayGrid == null) return;

            await overlayGrid.FadeTo(0, 200, Easing.CubicIn);

            overlayGrid.IsVisible = false;

            if (MainGrid != null && MainGrid.Children.Contains(overlayGrid))
            {
                MainGrid.Children.Remove(overlayGrid);
            }

            overlayGrid = null;
            isOverlayVisible = false;
        }
    }
}