using Application.Dto;

/// <summary>
/// Interface defining the contract for blog post business logic operations.
/// Provides methods for CRUD operations and advanced search/filtering capabilities.
/// </summary>
public interface IBlogPostService
{
    /// <summary>
    /// Creates a new article.
    /// </summary>
    Task CreateArticleAsync(BlogPostDto post);

    /// <summary>
    /// Retrieves all articles.
    /// </summary>
    Task<List<BlogPostDto>> GetAllArticlesAync();

    /// <summary>
    /// Searches for an article by its ID.
    /// </summary>
    Task<BlogPostDto?> SearchById(string id);

    /// <summary>
    /// Deletes an article by ID.
    /// </summary>
    Task DeleteArticleAsync(string id);

    /// <summary>
    /// Updates an existing article with new data.
    /// </summary>
    Task UpdatePostAsync(string id, BlogPostDto dto);

    /// <summary>
    /// Searches for articles by title.
    /// </summary>
    Task<List<BlogPostDto>> SearchByTitleAsync(string title);

    /// <summary>
    /// Searches for articles by content.
    /// </summary>
    Task<List<BlogPostDto>> SearchByContentAsync(string content);

    /// <summary>
    /// Searches for articles created on a specific date.
    /// </summary>
    Task<List<BlogPostDto>> SearchByDateAsync(DateOnly date);

    /// <summary>
    /// Searches for articles created within a date range.
    /// </summary>
    Task<List<BlogPostDto>> SearchByPeriodAsync(DateOnly startPeriod, DateOnly endPeriod);

    /// <summary>
    /// Counts articles created on a specific date.
    /// </summary>
    Task<int> CountByDate(DateOnly date);
}