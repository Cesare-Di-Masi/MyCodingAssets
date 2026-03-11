using Application.Dto;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Presentation_WPF.ViewModels
{
    /// <summary>
    /// Main view model managing the blog article list, search, filtering, and CRUD operations.
    /// Uses the MVVM Toolkit for property binding and command management.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly IBlogPostService _blogService;

        [ObservableProperty]
        private ObservableCollection<BlogPostDto> _articles = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsArticleSelected))]
        [NotifyCanExecuteChangedFor(nameof(UpdateArticleCommand), nameof(DeleteArticleCommand))]
        private BlogPostDto? _selectedArticle;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddArticleCommand))]
        private string _newTitle = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddArticleCommand))]
        private string _newContent = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _searchTitle = string.Empty;

        [ObservableProperty]
        private string _searchContent = string.Empty;

        [ObservableProperty]
        private DateTime? _searchDate = DateTime.Today;

        [ObservableProperty]
        private DateTime? _startDate = DateTime.Today.AddDays(-7);

        [ObservableProperty]
        private DateTime? _endDate = DateTime.Today;

        [ObservableProperty]
        private int _countForDate = 0;

        public bool IsArticleSelected => SelectedArticle != null;

        /// <summary>
        /// Initializes the view model with the blog post service and loads articles on startup.
        /// </summary>
        public MainViewModel(IBlogPostService blogService)
        {
            _blogService = blogService;
            _ = LoadArticlesAsync();
        }

        /// <summary>
        /// Loads all articles from the blog service and populates the Articles collection.
        /// Sets loading state and status messages for user feedback.
        /// </summary>
        [RelayCommand]
        private async Task LoadArticlesAsync()
        {
            IsLoading = true;
            StatusMessage = "Loading articles...";
            try
            {
                var articles = await _blogService.GetAllArticlesAync();
                Articles = new ObservableCollection<BlogPostDto>(articles);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading articles: {ex.Message}";
            }
            finally
            {
                StatusMessage = $"Loaded {Articles.Count} articles.";
                IsLoading = false;
            }
        }

        /// <summary>
        /// Creates a new article with the provided title and content.
        /// Validates input fields and reloads the article list after creation.
        /// Can only execute if both title and content are non-empty.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCreateArticle))]
        private async Task AddArticleAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTitle) || string.IsNullOrWhiteSpace(NewContent))
            {
                StatusMessage = "Title and content cannot be empty.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Creating article...";
            try
            {
                var newArticle = new BlogPostDto(NewTitle, NewContent);
                await _blogService.CreateArticleAsync(newArticle);
                Articles.Insert(0, newArticle);
                NewTitle = string.Empty;
                NewContent = string.Empty;
                StatusMessage = "Article created successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating article: {ex.Message}";
            }
            finally
            {
                await LoadArticlesAsync();
                IsLoading = false;
            }
        }

        /// <summary>
        /// Deletes the currently selected article.
        /// Requires an article to be selected before execution.
        /// </summary>
        [RelayCommand]
        private async Task DeleteArticleAsync()
        {
            if (SelectedArticle == null)
            {
                StatusMessage = "No article selected.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Deleting article...";
            try
            {
                await _blogService.DeleteArticleAsync(SelectedArticle.Id);
                Articles.Remove(SelectedArticle);
                SelectedArticle = null;
                StatusMessage = "Article deleted successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting article: {ex.Message}";
            }
            finally
            {
                await LoadArticlesAsync();
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates the currently selected article with new title and content.
        /// Requires an article to be selected and both fields to contain valid data.
        /// </summary>
        [RelayCommand]
        private async Task UpdateArticleAsync()
        {
            if (SelectedArticle == null)
            {
                StatusMessage = "No article selected.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewTitle) || string.IsNullOrWhiteSpace(NewContent))
            {
                StatusMessage = "Title and content cannot be empty.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Updating article...";
            try
            {
                var updatedArticle = new BlogPostDto(NewTitle, NewContent) { Id = SelectedArticle.Id };
                await _blogService.UpdatePostAsync(SelectedArticle.Id, updatedArticle);
                var index = Articles.IndexOf(SelectedArticle);
                if (index >= 0)
                {
                    Articles[index] = updatedArticle;
                }
                SelectedArticle = updatedArticle;
                StatusMessage = "Article updated successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating article: {ex.Message}";
            }
            finally
            {
                await LoadArticlesAsync();
                IsLoading = false;
            }
        }

        /// <summary>
        /// Searches for articles by title using the provided search term.
        /// Replaces the current article list with filtered results.
        /// </summary>
        [RelayCommand]
        private async Task SearchByTitleAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchTitle))
            {
                StatusMessage = "Enter a title to search.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Searching by title...";
            try
            {
                var results = await _blogService.SearchByTitleAsync(SearchTitle);
                Articles = new ObservableCollection<BlogPostDto>(results);
                StatusMessage = $"Found {Articles.Count} articles by title.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching by title: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Searches for articles by content using the provided search term.
        /// Replaces the current article list with filtered results.
        /// </summary>
        [RelayCommand]
        private async Task SearchByContentAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchContent))
            {
                StatusMessage = "Enter content text to search.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Searching by content...";
            try
            {
                var results = await _blogService.SearchByContentAsync(SearchContent);
                Articles = new ObservableCollection<BlogPostDto>(results);
                StatusMessage = $"Found {Articles.Count} articles by content.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching by content: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Searches for articles created on the specified date.
        /// Replaces the current article list with filtered results.
        /// </summary>
        [RelayCommand]
        private async Task SearchByDateAsync()
        {
            if (SearchDate == null)
            {
                StatusMessage = "Select a date to search.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Searching by date...";
            try
            {
                var dateOnly = DateOnly.FromDateTime(SearchDate.Value.Date);
                var results = await _blogService.SearchByDateAsync(dateOnly);
                Articles = new ObservableCollection<BlogPostDto>(results);
                StatusMessage = $"Found {Articles.Count} articles for date {dateOnly}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching by date: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Searches for articles created within the specified date range (start and end dates inclusive).
        /// Replaces the current article list with filtered results.
        /// </summary>
        [RelayCommand]
        private async Task SearchByPeriodAsync()
        {
            if (StartDate == null || EndDate == null)
            {
                StatusMessage = "Select start and end dates for the period.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Searching by period...";
            try
            {
                var start = DateOnly.FromDateTime(StartDate.Value.Date);
                var end = DateOnly.FromDateTime(EndDate.Value.Date);
                var results = await _blogService.SearchByPeriodAsync(start, end);
                Articles = new ObservableCollection<BlogPostDto>(results);
                StatusMessage = $"Found {Articles.Count} articles between {start} and {end}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching by period: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Counts the number of articles created on the specified date.
        /// Updates the CountForDate property with the result.
        /// </summary>
        [RelayCommand]
        private async Task CountByDateAsync()
        {
            if (SearchDate == null)
            {
                StatusMessage = "Select a date to count articles.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Counting articles by date...";
            try
            {
                var dateOnly = DateOnly.FromDateTime(SearchDate.Value.Date);
                CountForDate = await _blogService.CountByDate(dateOnly);
                StatusMessage = $"Articles on {dateOnly}: {CountForDate}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error counting articles: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Clears all search and filter criteria and reloads the complete article list.
        /// </summary>
        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            SearchTitle = string.Empty;
            SearchContent = string.Empty;
            SearchDate = DateTime.Today;
            StartDate = DateTime.Today.AddDays(-7);
            EndDate = DateTime.Today;
            CountForDate = 0;
            await LoadArticlesAsync();
        }

        /// <summary>
        /// Determines whether the AddArticleCommand can execute based on the validation of title and content fields.
        /// </summary>
        private bool CanCreateArticle() =>
            !string.IsNullOrWhiteSpace(NewTitle) && !string.IsNullOrWhiteSpace(NewContent);
    }
}