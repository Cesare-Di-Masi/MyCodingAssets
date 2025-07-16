using BattagliaNavale.Infrastucture;
using BattagliaNavale.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BattagliaNavale.ViewModels
{
    public partial class VisualizzatoreCampoViewModel : ObservableObject
    {
        public PartitaStatistiche partitaCorrente;

        public VisualizzatoreCampoViewModel(int id)
        {
            partitaCorrente = PreferencesUtilities.GetField(id);
        }


    }
}
