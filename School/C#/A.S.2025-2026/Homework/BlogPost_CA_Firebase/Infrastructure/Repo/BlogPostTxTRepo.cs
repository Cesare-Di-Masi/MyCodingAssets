using Infrastructure.Dto;
using Infrastructure.Mapper;
using System.Text;

namespace Infrastructure.Repo
{
    /// <summary>
    /// Repository implementation for text file-based persistence of blog posts.
    /// Uses custom delimiters to serialize and deserialize blog posts from a TXT file.
    /// Handles thread-safe file operations using semaphore locking.
    /// </summary>
    public class BlogPostTxTRepo : BlogPostBaseRepo
    {
        private const string VarSep = "$-$";
        private const string ObjSep = "$======END======$";
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Initializes the TXT repository with an optional file path.
        /// Uses "blogpost.txt" as the default file name if no path is provided.
        /// </summary>
        public BlogPostTxTRepo(string? filepath = null) : base(null, "blogpost.txt")
        {
        }

        /// <summary>
        /// Loads all blog posts from the TXT file into the cache.
        /// Parses the custom delimiter format and converts persistence DTOs to domain entities.
        /// Creates an empty cache if the file does not exist.
        /// Throws FormatException if the TXT format is invalid.
        /// Uses thread-safe semaphore locking to prevent concurrent access issues.
        /// </summary>
        protected override async Task LoadArticlesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(_filePath))
                {
                    _cache = new();
                    return;
                }

                var raw = await File.ReadAllTextAsync(_filePath);
                var objects = raw.Split(ObjSep, StringSplitOptions.RemoveEmptyEntries);
                var dtos = new List<BlogPostPersistenceDto>();

                foreach (var obj in objects)
                {
                    var parts = obj.Split(VarSep);

                    if (parts.Length != 4)
                        throw new FormatException("Invalid TXT format");

                    dtos.Add(new BlogPostPersistenceDto(
                        parts[0],
                        parts[1],
                        long.Parse(parts[2]),
                        parts[3]
                    ));
                }

                _cache = dtos.Select(d => d.ToEntity()).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Saves all blog posts from the cache to the TXT file.
        /// Serializes each blog post using the custom delimiter format.
        /// Uses thread-safe semaphore locking to prevent concurrent access issues.
        /// </summary>
        protected override async Task SaveAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var sb = new StringBuilder();

                foreach (var post in _cache)
                {
                    var dto = post.ToPersistenceDto();

                    sb.Append(dto.Title).Append(VarSep)
                      .Append(dto.Content).Append(VarSep)
                      .Append(dto.timestamp).Append(VarSep)
                      .Append(dto.Id)
                      .Append(ObjSep);
                }

                await File.WriteAllTextAsync(_filePath, sb.ToString());
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}