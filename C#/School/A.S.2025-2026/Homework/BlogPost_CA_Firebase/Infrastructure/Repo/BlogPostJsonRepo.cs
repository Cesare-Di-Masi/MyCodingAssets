using Domain.Model.Entities;
using Infrastructure.Dto;
using Infrastructure.Mapper;
using System.Text.Json;

namespace Infrastructure.Repo
{
    /// <summary>
    /// Repository implementation for JSON-based persistence of blog posts.
    /// Handles loading and saving blog posts from/to JSON files with thread-safe operations.
    /// </summary>
    public class BlogPostJsonRepo : BlogPostBaseRepo
    {
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Initializes the JSON repository with an optional file path.
        /// Uses "blogpost.json" as the default file name if no path is provided.
        /// </summary>
        public BlogPostJsonRepo(string? filepath = null) : base(null, "blogpost.json")
        {
        }

        /// <summary>
        /// Loads all blog posts from the JSON file into the cache.
        /// Creates an empty cache if the file does not exist.
        /// Uses thread-safe semaphore locking to prevent concurrent access issues.
        /// </summary>
        protected override async Task LoadArticlesAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!File.Exists(_filePath))
                {
                    _cache = new List<BlogPost>();
                    return;
                }

                await using var stream = new FileStream(
                    _filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

                var dtos = await JsonSerializer.DeserializeAsync<List<BlogPostPersistenceDto>>(stream)
                            .ConfigureAwait(false)
                           ?? new List<BlogPostPersistenceDto>();

                _cache = dtos.Select(d => d.ToEntity()).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Saves all blog posts from the cache to the JSON file.
        /// Uses thread-safe semaphore locking to prevent concurrent access issues.
        /// Overwrites the existing file with formatted JSON content.
        /// </summary>
        protected override async Task SaveAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var dtos = _cache.Select(p => p.ToPersistenceDto()).ToList();

                await using var stream = new FileStream(
                    _filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true);

                await JsonSerializer.SerializeAsync(stream, dtos,
                    new JsonSerializerOptions { WriteIndented = true })
                    .ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}