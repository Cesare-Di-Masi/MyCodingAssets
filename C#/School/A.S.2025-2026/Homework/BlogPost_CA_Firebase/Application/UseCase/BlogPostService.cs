using Application.Interface;
using Domain.Model.Entities;
using Application.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Mapper;

namespace Application.UseCase
{
    public class BlogPostService
    {

        private IBlogPostRepo _blogPostRepo;
        private List<BlogPost> blogPosts;
        public BlogPostService(IBlogPostRepo blogRepo) 
        {
            blogRepo = _blogPostRepo;
            blogPosts = LoadAsync().Result;
        }

        public void NewPost(BlogPostDto Post)
        {
            blogPosts.Add(Post.ToEntity());
            SaveAsync();
        }

        public List<BlogPostDto> GetAllPosts()
        {
            List<BlogPostDto> allPosts = blogPosts.Select(p => p.ToDto()).ToList();
            return allPosts;
        }

        public BlogPostDto? SearchById(Guid id)
        {
            foreach (var Post in blogPosts)
            {
                if (Post.Id == id)
                    return Post.ToDto();
            }
            return null;
        }

        public void RemovePost(Guid Id)
        {
            BlogPost? p =SearchById(Id).ToEntity();

            if (p != null)
            {
                blogPosts.Remove(p);
            }
            SaveAsync();
        }

        public void UpdatePost(Guid id, BlogPostDto dto)
        {
            BlogPost? p = SearchById(id).ToEntity();

            if(p==null)
            {
                throw new ArgumentException("Post does not exist");
            }

            p.ModifyTitle(dto.Title);
            p.ModifyContent(dto.Content);
            SaveAsync();

        }

        public async Task SaveAsync()
        {
            await _blogPostRepo.SaveAsync(blogPosts);
        }

        public async Task<List<BlogPost>> LoadAsync()
        {
            return await _blogPostRepo.LoadAsync();
        }

    }
}
