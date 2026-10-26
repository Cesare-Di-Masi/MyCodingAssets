using BattagliaNavale.Services;
using CommunityToolkit.Mvvm.Input;

namespace BattagliaNavale.Views;

public partial class Home : ContentPage
{
    public Home()
    {
        InitializeComponent();
    }

    [RelayCommand]
    protected override void OnAppearing()
    {
        AudioPlayerService.Instance.Stop();
        AudioPlayerService.Instance.Play("The Legend Of Monkey Island (Main Theme) - Sea Of Thieves Soundtrack.mp3");
    }

    [RelayCommand]
    public async Task OpenRegolamento()
    {
        await Navigation.PushAsync(new Regolamento());
    }

    [RelayCommand]
    public async Task OpenProfilo()
    {
        await Navigation.PushAsync(new CronologiaPartite());
    }

    [RelayCommand]
    public async Task OpenGioco()
    {
        await Navigation.PushAsync(new PosizionaNavi());
    }

    private void EsciClicked(object sender, EventArgs e)
    {
        Application.Current.Quit();
    }

    private void MusicaClicked(object sender, EventArgs e)
    {
        AudioPlayerService.Instance.Mute();

        if (AudioPlayerService.Instance.IsMuted)
        {
            btnMusica.Source = "../Resources/Images/musica_sbarrata.png";
        }
        else
        {
            btnMusica.Source = "../Resources/Images/musica.png";
        }
    }
}