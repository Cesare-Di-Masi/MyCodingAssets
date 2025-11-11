using Application.Dto;
using Application.UseCases;
using System.Windows;

namespace Presentation
{
    /// <summary>
    /// Logica di interazione per AddCatWindow.xaml
    /// </summary>
    public partial class AddCatWindow : Window
    {
        public CatteryService CatteryService;

        public AddCatWindow(CatteryService cattery)
        {
            InitializeComponent();
            CatteryService = cattery;
        }

        public void BtnAddCat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateOnly? birthDate = DateOnly.FromDateTime(DatePickerBirthdate.SelectedDate ?? DateTime.Now);
                if (birthDate == DateOnly.FromDateTime(DateTime.Now))
                    birthDate = null;

                CatteryService.RegisterNewCat
                    (
                    new CatDto(
                    Name: TxTBoxCatName.Text,
                    IsMale: (bool)IsMaleCheckBox.IsChecked,
                    ArrivingDate: DateOnly.FromDateTime(DatePickerArrivedIn.SelectedDate ?? DateTime.Now.AddDays(-1)),
                    BirthDate: birthDate,
                    Description: TxTBoxDescription.Text,
                    BreedName: TxTBoxBreed.Text,
                    Id: null
                    )
                    );
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            MessageBox.Show("Cat added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            BackToMain();
        }

        public void BtnClearCat_Click(object sender, RoutedEventArgs e)
        {
            TxTBoxCatName.Clear();
            IsMaleCheckBox.IsChecked = false;
            DatePickerArrivedIn.SelectedDate = null;
            DatePickerBirthdate.SelectedDate = null;
            TxTBoxDescription.Clear();
            TxTBoxBreed.Clear();
        }

        private void BackToMain()
        {
            var mainWindow = new MainWindow(CatteryService);
            mainWindow.Show();
            this.Close();
        }
    }
}