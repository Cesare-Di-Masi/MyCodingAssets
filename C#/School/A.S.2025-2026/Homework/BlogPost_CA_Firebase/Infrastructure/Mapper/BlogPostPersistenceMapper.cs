using Domain.Model.Entities;
using Infrastructure.Dto;

namespace Infrastructure.Mapper
{
    /// <summary>
    /// Mapper providing extension methods for converting between BlogPostPersistenceDto and BlogPost entities.
    /// Handles the conversion between persistence layer and domain layer, including timestamp serialization.
    /// </summary>
    public static class BlogPostPersistenceMapper
    {
        /// <summary>
        /// Converts a BlogPost domain entity to a BlogPostPersistenceDto for storage.
        /// Converts the creation date to Unix timestamp format for persistence.
        /// </summary>
        public static BlogPostPersistenceDto ToPersistenceDto(this BlogPost blogPost)
        {
            return new BlogPostPersistenceDto(
                blogPost.Title,
                blogPost.Content,
                ((DateTimeOffset)blogPost.CreatedAt).ToUnixTimeSeconds(),
                blogPost.Id.ToString()
            );
        }

        /// <summary>
        /// Converts a BlogPostPersistenceDto to a BlogPost domain entity.
        /// Returns null if the DTO is null, has an invalid ID, or has an invalid timestamp.
        /// Reconstructs the creation date from Unix timestamp format.
        /// </summary>
        public static BlogPost? ToEntity(this BlogPostPersistenceDto? dto)
        {
            if (dto is null)
                return null;

            if (string.IsNullOrEmpty(dto.Id))
                return null;

            if (!Guid.TryParse(dto.Id, out Guid guid))
                return null;

            try
            {
                var createdAt = DateTimeOffset.FromUnixTimeSeconds(dto.timestamp).DateTime;
                return new BlogPost(
                    dto.Title,
                    dto.Content,
                    createdAt,
                    guid
                );
            }
            catch
            {
                return null;
            }
        }
    }
}