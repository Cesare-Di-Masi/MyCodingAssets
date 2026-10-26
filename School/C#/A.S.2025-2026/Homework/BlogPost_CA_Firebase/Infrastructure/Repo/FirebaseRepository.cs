using Application.Interface;
using Domain.Model.Entities;
using Firebase.Database;
using Firebase.Database.Query;
using Infrastructure.Dto;
using Infrastructure.Mapper;

namespace Infrastructure.Repo
{
    /// <summary>
    /// non eredita da base repository datpo che è una classe che si distacca molto, questo
    /// perchè salva su un database e non in locale
    /// </summary>
    public class FirebaseRepository : IBlogPostRepo
    {
        private const string ArticlesNodes = "articles"; //base node for articles in firebase database
        private readonly FirebaseClient _client; //to connect to firebase database

        public FirebaseRepository(string firebaseUrl)//url of the firebase database
        {
            _client = new FirebaseClient(firebaseUrl); //initialize the FirebaseClient with the provided firebaseUrl
        }

        public Task<int> CountByDate(DateOnly date)
        {
            throw new NotImplementedException();
        }

        public async Task CreateArticleAsync(BlogPost dto)
        {
            var dtoP = dto.ToPersistenceDto();

            await _client
                .Child(ArticlesNodes) //bas node for saving the articles (if it doesn't exist it will be created)
                .Child(dto.Id.ToString()) //create a child node with the article id as the key (if it doesn't exist it will be created)
                .PutAsync(dtoP); //save the article data as a BlogPostPersistenceDto object at the specified location in the firebase database
        }

        public async Task DeleteArticleAsync(string id)
        {
            await _client
                .Child(ArticlesNodes)
                .Child(id) //navigate to the child node with the specified id
                .DeleteAsync(); //delete the article data at the specified location in the firebase database
        }

        public async Task<List<BlogPost>> GetAllArticlesAsync()
        {
            var dtos = await _client //connect to firebase database
                .Child(ArticlesNodes) //navigate to the "articles" node
                .OnceAsync<BlogPostPersistenceDto>(); //retrieve all articles as a list of BlogPostPersistenceDto objects at articles node

            return dtos
                .Select(d => d.Object.ToEntity()) // può restituire null
                .Where(p => p != null)
                .OrderByDescending(p => p!.CreatedAt)
                .Select(p => p!)
                .ToList();
        }

        public async Task<BlogPost?> GetArticleByIdAsync(string id)
        {
            var dtopP = await _client
                .Child(ArticlesNodes)
                .Child(id.ToString()) //navigate to the child node with the specified id
                .OnceSingleAsync<BlogPostPersistenceDto>(); //retrieve the article data as a BlogPostPersistenceDto object at the specified location in the firebase database

            // ToEntity è ora tollerante e può restituire null se dto mancante o non valido
            return dtopP.ToEntity();
        }

        public async Task<List<BlogPost>> GetByContent(string content)
        {
            var dtoP = await _client
                .Child(ArticlesNodes)
                .OnceAsync<BlogPostPersistenceDto>(); //retrieve all articles as a list of BlogPostPersistenceDto objects at articles node

            var entities = dtoP
                .Select(d => d.Object.ToEntity()) // può restituire null
                .Where(p => p != null && p.Content.Contains(content, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p!.CreatedAt)
                .Select(p => p!)
                .ToList();
            return entities;
        }

        public async Task<List<BlogPost>> GetByDate(DateOnly date)
        {
            var dtoP = await _client
                .Child(ArticlesNodes)
                .OnceAsync<BlogPostPersistenceDto>(); //retrieve all articles as a list of BlogPostPersistenceDto objects at articles node

            var entities = dtoP
                 .Select(d => d.Object.ToEntity()) // può restituire null
                 .Where(p => p != null && DateOnly.FromDateTime(p.CreatedAt) == date)
                .OrderByDescending(p => p!.CreatedAt)
                .Select(p => p!)
                .ToList();
            return entities;
        }

        public async Task<List<BlogPost>> GetByPeriod(DateOnly startPeriod, DateOnly endPeriod)
        {
            var dtoP = await _client
                .Child(ArticlesNodes)
                .OnceAsync<BlogPostPersistenceDto>(); //retrieve all articles as a list of BlogPostPersistenceDto objects at articles node

            var entities = dtoP.Select(d => d.Object.ToEntity());

            if (entities == null)
                return new List<BlogPost>();

            return entities.Where(p =>
                DateOnly.FromDateTime(p.CreatedAt) >= startPeriod &&
                DateOnly.FromDateTime(p.CreatedAt) <= endPeriod
            ).ToList();
        }

        public async Task<List<BlogPost>> GetByTitleAsync(string title)
        {
            var dtoP = await _client
                .Child(ArticlesNodes)
                .OnceAsync<BlogPostPersistenceDto>(); //retrieve all articles as a list of BlogPostPersistenceDto objects at articles node

            var entities = dtoP
                .Select(d => d.Object.ToEntity()) // può restituire null
                .Where(p => p != null && p.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p!.CreatedAt)
                .Select(p => p!)
                .ToList();
            return entities;
        }

        public async Task UpdateArticleAsync(string id, BlogPost dto)
        {
            var dtoP = dto.ToPersistenceDto();
            await _client
                .Child(ArticlesNodes)
                .Child(id)
                .PutAsync(dtoP);
        }
    }
}