using Presentation_WPF.ViewModels;
using System.Windows;

namespace Presentation_WPF.Views
{
    /// <summary>
    /// Dialog window for creating a new blog article.
    /// Captures article title and content from user input and passes them to the view model for processing.
    /// </summary>
    public partial class AddArticleWindow : Window
    {
        /// <summary>
        /// Initializes the AddArticleWindow.
        /// </summary>
        public AddArticleWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Validates the input fields, passes the title and content to the parent window's view model,
        /// and executes the AddArticleCommand to save the new article.
        /// Closes the window after saving or displays an error message if the view model cannot be accessed.
        /// </summary>
        private void SaveAddedArticle(object sender, RoutedEventArgs e)
        {
            var title = TitleTextBox.Text ?? string.Empty;
            var content = ContentTextBox.Text ?? string.Empty;

            if (this.Owner?.DataContext is MainViewModel viewModel)
            {
                viewModel.NewTitle = title;
                viewModel.NewContent = content;
                viewModel.AddArticleCommand.Execute(null);
            }
            else
            {
                MessageBox.Show(
                    "Error: Unable to find the main view model.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            this.Close();
        }

        /// <summary>
        /// Closes the AddArticleWindow without saving any data.
        /// </summary>
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}