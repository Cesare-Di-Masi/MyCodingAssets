using MastermindLib;
using System.Windows;

namespace MasterMind_DiMasi_Senni
{
    /// <summary>
    /// Logica di interazione per Window1.xaml
    /// </summary>
    public partial class GameSettings : Window
    {
        private bool _isColorBlind;

        public GameSettings(bool isColorBlind)
        {
            _isColorBlind = isColorBlind;
            InitializeComponent();
        }

<<<<<<< HEAD

=======
        private void btnConferma_Click(object sender, RoutedEventArgs e)
        {
            btnConferma.IsEnabled = _isColorBlind;
        }

        private void btnTornaIndietro_Click(object sender, RoutedEventArgs e)
        {
            var a = new PveWindows();
            a.Show();
            this.Close();
        }

        private void btnGioca_Click(object sender, RoutedEventArgs e)
        {
           throw new NotImplementedException();
        }
>>>>>>> 826b506f96a4a9138451c2649602744677f60461
    }
}