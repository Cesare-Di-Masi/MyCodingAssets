using Presentation_WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Presentation_WPF.Views
{
    /// <summary>
    /// Main application window providing the UI for managing blog articles.
    /// Handles user interactions and delegates operations to the MainViewModel.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes the MainWindow with the provided view model.
        /// </summary>
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        /// <summary>
        /// Opens the AddArticleWindow when the user clicks the "Add Article" button.
        /// Sets the owner window to allow the child window to access the parent's data context.
        /// </summary>
        private void AddArticleBtnClicked(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddArticleWindow();
            addWindow.Owner = this;
            addWindow.ShowDialog();
        }

        /// <summary>
        /// Updates the view model properties when the selected article in the ListBox changes.
        /// Populates the edit fields with the selected article's current values or clears them if no article is selected.
        /// </summary>
        private void ArticlesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                if (viewModel.SelectedArticle != null)
                {
                    viewModel.NewTitle = viewModel.SelectedArticle.Title ?? string.Empty;
                    viewModel.NewContent = viewModel.SelectedArticle.Content ?? string.Empty;
                }
                else
                {
                    viewModel.NewTitle = string.Empty;
                    viewModel.NewContent = string.Empty;
                }
            }
        }
    }
}