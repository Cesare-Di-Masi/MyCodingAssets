using System.Security.AccessControl;

namespace Domain.Model.Entities
{
    public class BlogPost
    {
        private string _title;
        private string _content;
        private Guid _id;
        private DateTime _createdAt;

        public string Title
        {
            get { return _title; }
            private set 
            {
                if(string.IsNullOrEmpty(_title))
                    throw new ArgumentNullException("title is not valid");
                _title = value; 
            }
        }

        public string Content
        {
            get { return _content; }

            private set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException("content is not valid");
                _content = value;
            }
        }

        public Guid Id
        {
            get { return _id; }
        }

        public DateTime CreatedAt
        { 
            get { return _createdAt; }
            private set 
            {
                if (value > DateTime.Now)
                    throw new ArgumentException("invalid creation date");
                _createdAt = value;
            }
        }

        public BlogPost(string title, string content) 
        {
            Title = title;
            Content = content;
            CreatedAt = DateTime.Now;
            _id = Guid.NewGuid();
        }

        public void ModifyTitle(string title)
        {
            Title = title;
        }

        public void ModifyContent(string content)
        {
            Content = content;
        }

        public BlogPost(string title, string content, DateTime createdAt, Guid id):this(title,content)  
        {
            CreatedAt = createdAt;
            _id=id;
        }

    }
}
