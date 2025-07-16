using CommunityToolkit.Mvvm.Input;

namespace BattagliaNavale.Views;

public partial class Regolamento : ContentPage
{
    public Regolamento()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public async Task OpenHome()
    {
        await Navigation.PopAsync();
    }

}