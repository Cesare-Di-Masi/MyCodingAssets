using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Dto;
using Domain.Model.Entities;

namespace Application.Mapper
{
    public static class BlogPostMapper
    {

        public static BlogPost ToEntity(this BlogPostDto dto)
        {
            return new BlogPost
                (
                dto.Title,
                dto.Content
                );
        }

        public static BlogPostDto ToDto(this BlogPost entity)
        {
            return new BlogPostDto
                (
                entity.Title,
                entity.Content,
                entity.CreatedAt,
                entity.Id
                );
        }

    }
}
