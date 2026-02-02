using Domain.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interface
{
    public interface IBlogPostRepo
    {
        public Task SaveAsync(List<BlogPost> posts);

        public Task<List<BlogPost>> LoadAsync();
    }
}
