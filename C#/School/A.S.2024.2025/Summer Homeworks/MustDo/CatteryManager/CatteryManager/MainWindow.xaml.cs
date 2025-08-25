using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CatteryManagerLib;

namespace CatteryManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Cattery _cattery;
        public MainWindow()
        {
            InitializeComponent();
            _cattery = LoadFromFile();
            if (_cattery == null)
                _cattery = new Cattery();
        }

        public MainWindow(Cattery cattery)
        {
            InitializeComponent();
            _cattery = cattery;
        }

        private Cattery LoadFromFile()
        {
            // Inizialmente carica il numero totale di gatti dal gestore dati
            return Serializer.GetFile();
        }

        private void LoadTotalCats()
        {
            if (_cattery != null)
            {
                int totalCats = _cattery.Cats.Count;
                txtTotalCats.Text = totalCats.ToString();
            }
            else
            {
                txtTotalCats.Text = "0";
            }
        }
        private void ButtonViewCats_Click(object sender, RoutedEventArgs e)
        {
            CatsWindow gattiWindow = new CatsWindow(_cattery);
            gattiWindow.ShowDialog();
            LoadTotalCats(); // Aggiorna numero totale se necessario
        }

        private void ButtonNewCat_Click(object sender, RoutedEventArgs e)
        {
            NewCatWindow nuovoGattoWindow = new NewCatWindow(_cattery);
            nuovoGattoWindow.ShowDialog();
            LoadTotalCats(); // Aggiorna numero totale
        }

        private void ButtonViewAdoptions_Click(object sender, RoutedEventArgs e)
        {
            AdoptionWindow adozioniWindow = new AdoptionWindow(_cattery);
            adozioniWindow.ShowDialog();
        }

        private void ButtonNewAdoption_Click(object sender, RoutedEventArgs e)
        {
            NewAdoptionWindow nuovaAdozioneWindow = new NewAdoptionWindow(_cattery);
            nuovaAdozioneWindow.ShowDialog();
            LoadTotalCats(); // Aggiorna se un gatto è uscito
        }

        private void ButtonFailedAdoption_Click(object sender, RoutedEventArgs e)
        {
            CanceledAdoptionWindow failedWindow = new CanceledAdoptionWindow(_cattery);
            failedWindow.ShowDialog();
            LoadTotalCats(); // Aggiorna se un gatto è tornato
        }
    }
}