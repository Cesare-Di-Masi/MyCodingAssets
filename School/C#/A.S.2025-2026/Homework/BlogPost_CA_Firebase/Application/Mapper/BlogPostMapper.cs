using Application.Dto;
using Domain.Model.Entities;

namespace Application.Mapper
{
    /// <summary>
    /// Mapper providing extension methods for converting between BlogPostDto and BlogPost entities.
    /// Handles the conversion between presentation and domain layers.
    /// </summary>
    public static class BlogPostMapper
    {
        /// <summary>
        /// Converts a BlogPostDto to a BlogPost domain entity.
        /// </summary>
        public static BlogPost ToEntity(this BlogPostDto dto)
        {
            return new BlogPost(dto.Title, dto.Content);
        }

        /// <summary>
        /// Converts a BlogPost domain entity to a BlogPostDto for presentation.
        /// </summary>
        public static BlogPostDto ToDto(this BlogPost entity)
        {
            return new BlogPostDto(
                entity.Title,
                entity.Content,
                entity.CreatedAt,
                entity.Id.ToString()
            );
        }
    }
}