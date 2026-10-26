using MastermindLib;
using System.Windows;

namespace MasterMind_DiMasi_Senni
{
    /// <summary>
    /// Logica di interazione per PveWindows.xaml
    /// </summary>
    public partial class PveWindows : Window
    {
        private bool _isColorBlind = true;
        private GameManager game;
        public EasiestGenerator generator = new EasiestGenerator();

        public PveWindows()
        {
            InitializeComponent();
        }

        private void btnMenù_Click(object sender, RoutedEventArgs e)
        {
            var a = new MainWindow();
            a.Show();
            this.Close();
        }

        private void btnEasy_Click(object sender, RoutedEventArgs e)
        {
            game = new GameManager(_isColorBlind, 4, 4, 5, 1);
            Window gameWindow = new GameWindow(game);
            //gameWindow.Show();
            this.Close();
        }

        private void btnDifficult_Click(object sender, RoutedEventArgs e)
        {
            game = new GameManager(_isColorBlind, 10, 6, 5, 2);
            Window Game = new GameWindow(game);
            Game.Show();
            this.Close();
        }

        private void btnMedium_Click(object sender, RoutedEventArgs e)
        {
            game = new GameManager(_isColorBlind, 4, 4, 5, 4, generator);
            Window gameWindow = new GameWindow(game);
            gameWindow.Show();
            this.Close();
        }

        private void btnPersonalized_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnPersonalized_Click_1(object sender, RoutedEventArgs e)
        {
        }

        private void btnPersonalized_Click_2(object sender, RoutedEventArgs e)
        {
            var a = new GameSettings(_isColorBlind);
            a.Show();
            this.Close();
        }
    }
}