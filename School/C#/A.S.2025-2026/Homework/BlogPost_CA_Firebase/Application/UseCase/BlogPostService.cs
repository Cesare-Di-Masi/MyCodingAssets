using Application.Dto;
using Application.Interface;
using Application.Mapper;

namespace Application.UseCase
{
    /// <summary>
    /// Service handling all blog post business logic operations.
    /// Provides CRUD operations and advanced search/filtering capabilities through the repository abstraction.
    /// </summary>
    public class BlogPostService : IBlogPostService
    {
        private readonly IBlogPostRepo _blogPostRepo;

        /// <summary>
        /// Initializes the service with a blog post repository dependency.
        /// </summary>
        public BlogPostService(IBlogPostRepo blogRepo)
        {
            _blogPostRepo = blogRepo;
        }

        /// <summary>
        /// Creates a new article by converting the DTO to a domain entity and storing it in the repository.
        /// </summary>
        public async Task CreateArticleAsync(BlogPostDto post)
        {
            await _blogPostRepo.CreateArticleAsync(post.ToEntity());
        }

        /// <summary>
        /// Retrieves all articles from the repository and converts them to DTOs for presentation.
        /// </summary>
        public async Task<List<BlogPostDto>> GetAllArticlesAync()
        {
            var entities = await _blogPostRepo.GetAllArticlesAsync();
            return entities.Select(e => e.ToDto()).ToList();
        }

        /// <summary>
        /// Searches for an article by its ID and converts it to a DTO if found.
        /// Returns null if no article matches the provided ID.
        /// </summary>
        public async Task<BlogPostDto?> SearchById(string id)
        {
            var entity = await _blogPostRepo.GetArticleByIdAsync(id);
            return entity?.ToDto();
        }

        /// <summary>
        /// Deletes an article by ID after verifying it exists.
        /// Throws InvalidOperationException if the article is not found.
        /// </summary>
        public async Task DeleteArticleAsync(string id)
        {
            var entity = await _blogPostRepo.GetArticleByIdAsync(id);
            if (entity == null)
                throw new InvalidOperationException("Article not found");
            await _blogPostRepo.DeleteArticleAsync(id);
        }

        /// <summary>
        /// Updates an existing article with new title and content.
        /// Throws InvalidOperationException if the article to update does not exist.
        /// </summary>
        public async Task UpdatePostAsync(string id, BlogPostDto dto)
        {
            var entity = await _blogPostRepo.GetArticleByIdAsync(id);
            if (entity == null)
                throw new InvalidOperationException("Article not found");
            await _blogPostRepo.UpdateArticleAsync(id, dto.ToEntity());
        }

        /// <summary>
        /// Searches for articles by title and returns matching results as DTOs.
        /// </summary>
        public async Task<List<BlogPostDto>> SearchByTitleAsync(string title)
        {
            var entities = await _blogPostRepo.GetByTitleAsync(title);
            return entities.Select(e => e.ToDto()).ToList();
        }

        /// <summary>
        /// Searches for articles by content and returns matching results as DTOs.
        /// </summary>
        public async Task<List<BlogPostDto>> SearchByContentAsync(string content)
        {
            var entities = await _blogPostRepo.GetByContent(content);
            return entities.Select(e => e.ToDto()).ToList();
        }

        /// <summary>
        /// Searches for articles created on the specified date and returns matching results as DTOs.
        /// </summary>
        public async Task<List<BlogPostDto>> SearchByDateAsync(DateOnly date)
        {
            var entities = await _blogPostRepo.GetByDate(date);
            return entities.Select(e => e.ToDto()).ToList();
        }

        /// <summary>
        /// Searches for articles created within the specified date range (inclusive) and returns matching results as DTOs.
        /// </summary>
        public async Task<List<BlogPostDto>> SearchByPeriodAsync(DateOnly startPeriod, DateOnly endPeriod)
        {
            var entities = await _blogPostRepo.GetByPeriod(startPeriod, endPeriod);
            return entities.Select(e => e.ToDto()).ToList();
        }

        /// <summary>
        /// Counts the number of articles created on the specified date.
        /// </summary>
        public async Task<int> CountByDate(DateOnly date)
        {
            return await _blogPostRepo.CountByDate(date);
        }
    }
}