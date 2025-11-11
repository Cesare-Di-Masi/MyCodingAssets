using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Application.Dto;
using Application.UseCases;

namespace Presentation
{
    /// <summary>
    /// Logica di interazione per CatManagerWindow.xaml
    /// </summary>
    public partial class CatManagerWindow : Window
    {
        public CatteryService CatteryService;

        public CatManagerWindow(CatteryService cattery)
        {
            InitializeComponent();
            CatteryService = cattery;
            dgCats.ItemsSource = CatteryService.ViewAllCats();
        }

        public void BtnBackToMain_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow(CatteryService);
            mainWindow.Show();
            this.Close();
        }

        public void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            dgCats.ItemsSource = CatteryService.ViewAllCats();
        }
    }
}