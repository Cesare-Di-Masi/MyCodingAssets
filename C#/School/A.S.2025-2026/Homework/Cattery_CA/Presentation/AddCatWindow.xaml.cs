using Application.Dto;
using Application.UseCases;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Presentation
{
    /// <summary>
    /// Logica di interazione per AddCatWindow.xaml
    /// </summary>
    public partial class AddCatWindow : Window
    {
        public CatteryService CatteryService;
        private bool isEditMode = false;
        private string id = string.Empty;

        public AddCatWindow(CatteryService cattery)
        {
            InitializeComponent();
            CatteryService = cattery;
            isEditMode = false;
        }

        public AddCatWindow(CatteryService cattery, CatDto cat)
        {
            InitializeComponent();
            CatteryService = cattery;
            isEditMode = true;
            Window_Loaded(cat);
            id = cat.Id ?? string.Empty;
        }

        private void Window_Loaded(CatDto dto)
        {
            if (isEditMode && dto is not null)
            {
                TxTBoxCatName.Text = dto.Name;
                IsMaleCheckBox.IsChecked = dto.IsMale;
                DatePickerArrivedIn.SelectedDate = dto.ArrivingDate.ToDateTime(new TimeOnly(0, 0));
                if (dto.BirthDate.HasValue)
                    DatePickerBirthdate.SelectedDate = dto.BirthDate.Value.ToDateTime(new TimeOnly(0, 0));
                TxTBoxDescription.Text = dto.Description;
                TxTBoxBreed.Text = dto.BreedName;
            }
        }

        public void BtnAddCat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateOnly? birthDate = DateOnly.FromDateTime(DatePickerBirthdate.SelectedDate ?? DateTime.Now);
                if (birthDate == DateOnly.FromDateTime(DateTime.Now))
                    birthDate = null;

                CatDto dto =
                    new CatDto(
                    Name: TxTBoxCatName.Text,
                    IsMale: (bool)IsMaleCheckBox.IsChecked,
                    ArrivingDate: DateOnly.FromDateTime(DatePickerArrivedIn.SelectedDate ?? DateTime.Now.AddDays(-1)),
                    BirthDate: birthDate,
                    Description: TxTBoxDescription.Text,
                    BreedName: TxTBoxBreed.Text,
                    Id: id

                    );

                if (isEditMode)
                {
                    CatteryService.UpdateCat(dto);
                }
                else
                    CatteryService.RegisterNewCat(dto);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Cat added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            if (isEditMode)
                this.Close();
            else
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