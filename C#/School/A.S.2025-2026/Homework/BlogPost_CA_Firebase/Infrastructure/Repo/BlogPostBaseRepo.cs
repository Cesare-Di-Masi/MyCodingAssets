using Application.Interface;
using Domain.Model.Entities;

namespace Infrastructure.Repo
{
    /// <summary>
    /// Abstract base repository providing common CRUD and search operations for blog posts.
    /// Implements caching and thread-safe operations using semaphore.
    /// Derived classes implement format-specific persistence (JSON, TXT, Firebase, etc.).
    /// </summary>
    public abstract class BlogPostBaseRepo : IBlogPostRepo
    {
        protected readonly string _filePath;
        protected List<BlogPost> _cache = new();
        protected static readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Initializes the repository with a file path or default file name.
        /// Creates the necessary directory structure if it does not exist.
        /// </summary>
        public BlogPostBaseRepo(string? filepath = null, string? defaultFileName = null)
        {
            _filePath = filepath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlogProjects",
                defaultFileName
            );

            var directory = Path.GetDirectoryName(_filePath)!;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Abstract method for loading articles from the persistence source.
        /// Must be implemented by derived classes for their specific storage format.
        /// </summary>
        protected abstract Task LoadArticlesAsync();

        /// <summary>
        /// Abstract method for saving articles to the persistence source.
        /// Must be implemented by derived classes for their specific storage format.
        /// </summary>
        protected abstract Task SaveAsync();

        /// <summary>
        /// Creates and persists a new blog post.
        /// </summary>
        public async Task CreateArticleAsync(BlogPost entity)
        {
            await LoadArticlesAsync();
            _cache.Add(entity);
            await SaveAsync();
        }

        /// <summary>
        /// Deletes a blog post by ID from the cache and persistence.
        /// Returns silently if the post is not found.
        /// </summary>
        public async Task DeleteArticleAsync(string id)
        {
            await LoadArticlesAsync();

            var guid = Guid.Parse(id);
            var post = _cache.FirstOrDefault(p => p.Id == guid);
            if (post == null)
                return;

            _cache.Remove(post);
            await SaveAsync();
        }

        /// <summary>
        /// Retrieves all blog posts ordered by creation date in descending order.
        /// Returns a safe snapshot of the cache.
        /// </summary>
        public async Task<List<BlogPost>> GetAllArticlesAsync()
        {
            await LoadArticlesAsync();
            return _cache.OrderByDescending(p => p.CreatedAt).ToList();
        }

        /// <summary>
        /// Retrieves a blog post by ID. Returns null if not found.
        /// </summary>
        public async Task<BlogPost?> GetArticleByIdAsync(string id)
        {
            await LoadArticlesAsync();

            var guid = Guid.Parse(id);
            return _cache.FirstOrDefault(p => p.Id == guid);
        }

        /// <summary>
        /// Updates an existing blog post with new title and content.
        /// Throws KeyNotFoundException if the post to update does not exist.
        /// </summary>
        public async Task UpdateArticleAsync(string id, BlogPost entity)
        {
            await LoadArticlesAsync();

            var guid = Guid.Parse(id);
            var post = _cache.FirstOrDefault(p => p.Id == guid);
            if (post == null)
                throw new KeyNotFoundException($"Post {id} not found");

            post.ModifyTitle(entity.Title);
            post.ModifyContent(entity.Content);
            await SaveAsync();
        }

        /// <summary>
        /// Retrieves all blog posts matching the specified title.
        /// </summary>
        public async Task<List<BlogPost>> GetByTitleAsync(string title)
        {
            await LoadArticlesAsync();
            return _cache.Where(p => p.Title == title).ToList();
        }

        /// <summary>
        /// Retrieves all blog posts matching the specified content.
        /// </summary>
        public async Task<List<BlogPost>> GetByContent(string content)
        {
            await LoadArticlesAsync();
            return _cache.Where(p => p.Content == content).ToList();
        }

        /// <summary>
        /// Retrieves all blog posts created on the specified date.
        /// </summary>
        public async Task<List<BlogPost>> GetByDate(DateOnly date)
        {
            await LoadArticlesAsync();
            return _cache.Where(p => DateOnly.FromDateTime(p.CreatedAt) == date).ToList();
        }

        /// <summary>
        /// Retrieves all blog posts created within the specified date range (inclusive).
        /// </summary>
        public async Task<List<BlogPost>> GetByPeriod(DateOnly startPeriod, DateOnly endPeriod)
        {
            await LoadArticlesAsync();
            return _cache.Where(p =>
                DateOnly.FromDateTime(p.CreatedAt) >= startPeriod &&
                DateOnly.FromDateTime(p.CreatedAt) <= endPeriod
            ).ToList();
        }

        /// <summary>
        /// Counts the number of blog posts created on the specified date.
        /// </summary>
        public async Task<int> CountByDate(DateOnly date)
        {
            await LoadArticlesAsync();
            return _cache.Where(p => DateOnly.FromDateTime(p.CreatedAt) == date).Count();
        }
    }
}