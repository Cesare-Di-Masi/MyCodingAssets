using Domain.Model.Entities;

namespace Application.Interface
{
    /// <summary>
    /// Repository interface defining the contract for persistent storage operations on blog posts.
    /// Implementations handle data access and persistence logic.
    /// </summary>
    public interface IBlogPostRepo
    {
        /// <summary>
        /// Creates and stores a new blog post.
        /// </summary>
        Task CreateArticleAsync(BlogPost dto);

        /// <summary>
        /// Updates an existing blog post by ID with new data.
        /// </summary>
        Task UpdateArticleAsync(string id, BlogPost dto);

        /// <summary>
        /// Deletes a blog post by ID.
        /// </summary>
        Task DeleteArticleAsync(string id);

        /// <summary>
        /// Retrieves a blog post by ID. Returns null if not found.
        /// </summary>
        Task<BlogPost?> GetArticleByIdAsync(string id);

        /// <summary>
        /// Retrieves all blog posts.
        /// </summary>
        Task<List<BlogPost>> GetAllArticlesAsync();

        /// <summary>
        /// Retrieves blog posts matching a specific title.
        /// </summary>
        Task<List<BlogPost>> GetByTitleAsync(string title);

        /// <summary>
        /// Retrieves blog posts matching specific content.
        /// </summary>
        Task<List<BlogPost>> GetByContent(string content);

        /// <summary>
        /// Retrieves blog posts created on a specific date.
        /// </summary>
        Task<List<BlogPost>> GetByDate(DateOnly date);

        /// <summary>
        /// Retrieves blog posts created within a date range (inclusive).
        /// </summary>
        Task<List<BlogPost>> GetByPeriod(DateOnly startPeriod, DateOnly endPeriod);

        /// <summary>
        /// Counts blog posts created on a specific date.
        /// </summary>
        Task<int> CountByDate(DateOnly date);
    }
}