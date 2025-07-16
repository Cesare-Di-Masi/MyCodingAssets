using BattagliaNavale.Models;
using BattagliaNavale.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace BattagliaNavale.Views;

public partial class VisualizzatoreCampo : ContentPage
{
    const int LUNGHEZZA = 10;

    public VisualizzatoreCampo(int id)
    {
        InitializeComponent();

        BindingContext = new ViewModels.VisualizzatoreCampoViewModel(id);

        var vm = (VisualizzatoreCampoViewModel)BindingContext;

        lblNumPartita.Text = $"Partita numero {vm.partitaCorrente.Id}";

        lblTempoPartita.Text = "Durata partita: " + vm.partitaCorrente.TempoPartitaTesto;

        lblVincitorePartita.Text = "Il vincore è il " + vm.partitaCorrente.RisultatoPartita;

        SetupGrid(grdCampoBot);
        SetupGrid(grdCampoPlayer);

        CreateGridCells(grdCampoBot);
        CreateGridCells(grdCampoPlayer);

        AddShips(grdCampoBot, vm.partitaCorrente.BarcheBot);
        AddShips(grdCampoPlayer, vm.partitaCorrente.BarchePlayer);

        AddFeedback(grdCampoBot, vm.partitaCorrente.CampoBot);
        AddFeedback(grdCampoPlayer, vm.partitaCorrente.CampoPlayer);
    }

    private void SetupGrid(Grid grid)
    {
        for (int i = 0; i < LUNGHEZZA; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }
    }

    private void CreateGridCells(Grid grid)
    {
        for (int i = 0; i < LUNGHEZZA; i++)
        {
            for (int j = 0; j < LUNGHEZZA; j++)
            {
                var cella = new Microsoft.Maui.Controls.Shapes.Rectangle
                {
                    Fill = new SolidColorBrush(Colors.LightBlue),
                    Stroke = new SolidColorBrush(Colors.CadetBlue),
                    StrokeThickness = 1
                };
                Grid.SetColumn(cella, j);
                Grid.SetRow(cella, i);
                grid.Children.Add(cella);
            }
        }
    }

    private void AddShips(Grid grid, List<PosizioneBarca> barche)
    {
        for (int i = 0; i < barche.Count(); i++)
        {
            int lunghezza = GetShipLength(i);
            string source = GetShipImageSource(i);
            double scale = GetShipScale(i);

            var barca = new Image
            {
                Source = ImageSource.FromFile(source),
                ZIndex = 1,
                Rotation = barche[i].Verticale ? 90 : 0,
                Scale = barche[i].Verticale ? scale : 1.0,
                Aspect = Aspect.AspectFit
            };

            Grid.SetRow(barca, barche[i].X);
            Grid.SetColumn(barca, barche[i].Y);

            if (barche[i].Verticale)
                Grid.SetColumnSpan(barca, lunghezza);
            else
                Grid.SetRowSpan(barca, lunghezza);

            grid.Children.Add(barca);
        }
    }

    private int GetShipLength(int shipIndex)
    {
        return shipIndex switch
        {
            0 => 2,
            1 => 3,
            2 => 3,
            3 => 4,
            _ => 2
        };
    }

    private string GetShipImageSource(int shipIndex)
    {
        return shipIndex switch
        {
            0 => "../Resources/Images/barca3.png",
            1 => "../Resources/Images/barca1.png",
            2 => "../Resources/Images/barca4.png",
            3 => "../Resources/Images/barca2.png",
            _ => "../Resources/Images/barca3.png"
        };
    }

    private double GetShipScale(int shipIndex)
    {
        return shipIndex switch
        {
            0 => 2.0,
            1 => 3.0,
            2 => 2.7,
            3 => 3.7,
            _ => 1.0
        };
    }

    private void AddFeedback(Grid grid, List<CellaCampo> campo)
    {
        foreach (var cella in campo)
        {
            if (cella.Stato == StatoCampo.NAVE_COLPITA)
            {
                var imgFeedback = new Image
                {
                    Source = ImageSource.FromFile("../Resources/Images/fire.gif"),
                    ZIndex = 2,
                    IsAnimationPlaying = true,
                    WidthRequest = 40,
                    HeightRequest = 40,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                Grid.SetRow(imgFeedback, cella.X);
                Grid.SetColumn(imgFeedback, cella.Y);
                grid.Children.Add(imgFeedback);
            }
            else if (cella.Stato == StatoCampo.ACQUA_COLPITA)
            {
                var imgFeedback = new Image
                {
                    Source = ImageSource.FromFile("../Resources/Images/croce_img.png"),
                    ZIndex = 2,
                    WidthRequest = 40,
                    HeightRequest = 40,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                Grid.SetRow(imgFeedback, cella.X);
                Grid.SetColumn(imgFeedback, cella.Y);
                grid.Children.Add(imgFeedback);
            }
        }
    }

    [RelayCommand]
    public async Task Esci()
    {
        await Navigation.PopAsync();
    }
}