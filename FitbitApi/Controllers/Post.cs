using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Net.Http;
using System.Collections;
using System.Web;

namespace FitbitApi.Controllers {
    public class PostController : ApiController {
        private int _timeout;
        private List<BlogPost> _posts = new List<BlogPost> {
                new BlogPost { Id = 1, Title = "Developer Story", Summary = "This is the super summary...", Content = "Lorem ipsum ..." },
                new BlogPost { Id = 2, Title = "Engineering Interview", Summary = "Once upon a time...", Content = "This is the content..." },
            };

        public PostController() {
            int timeout = int.TryParse(ConfigurationManager.AppSettings["NotesCacheTimeoutInSeconds"], out timeout) ? timeout : 15; // default timeout
            _timeout = timeout;
        }

        [HttpGet, Route("posts")]
        public IHttpActionResult GetPosts() {
            return Ok(_posts);
        }

        [HttpGet, Route("posts/{id}")]
        public IHttpActionResult GetPosts(int id) {
            var post = _posts.Where(x => x.Id == id).FirstOrDefault();
            if (post == null)
                return NotFound();
            else
                return Ok(post);
        }

        [HttpPost, Route("posts")]
        public IHttpActionResult SavePost(BlogPost post) {
            var updatedPost = SaveBlogPost(post);
            return Ok(updatedPost);
        }

        [HttpPut, Route("posts/{id}")]
        public IHttpActionResult UpdatePost(BlogPost post, [FromUri] int id) {
            var updatedPost = SaveBlogPost(post, isUpdate: true, id: id);
            return Ok(updatedPost);
        }

        [HttpDelete, Route("posts/{id}")]
        public HttpResponseMessage DeletePost(int id) {
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        private BlogPost SaveBlogPost(BlogPost post, bool? isUpdate = false, int? id = 0) {
            if (isUpdate == true) {
                var p = _posts.First(x => x.Id == id);
                p.Title = post.Title ?? p.Title;
                p.Summary = post.Summary ?? p.Summary;
                p.Content = post.Content ?? p.Content;
                // ... update the rest of the post.
                p.ModifiedOn = DateTime.Now;
                p.Tags = post.Tags;
                p.RelatedPosts = post.RelatedPosts;
                return p;
            }
            else {
                post.Id = _posts.Max(x => x.Id) + 1;
                post.CreatedBy = 1;
                post.CreatedOn = DateTime.Now;
                _posts.Add(post);
                return post;
            }
        }

        private IEnumerable<BlogPost> GetBlogPostsFromStorage() {
            var posts = HttpRuntime
                .Cache.GetOrStore<IEnumerable<BlogPost>>(
                    key: "abc",
                    expiration: new TimeSpan(0, 0, 0),
                    generator: () => { return _posts; }
                );

            return posts;
        }
    }

    public class CommentController : ApiController {
        private List<BlogPost> _posts = new List<BlogPost> {
                new BlogPost {
                    Id = 1,
                    Title = "Developer Story",
                    Summary = "This is the super summary...",
                    Content = "Lorem ipsum ...",
                    Comments = new List<Comment> {
                        new Comment { Id = 1, Content = "This is the 1st comment's content.", PostId = 1 },
                        new Comment { Id = 2, Content = "This is the 2nd comment's content.", PostId = 1 },
                        new Comment { Id = 3, Content = "This is the 3th comment's content.", PostId = 1 },
                        new Comment { Id = 4, Content = "This is the 4th comment's content.", PostId = 1 },
                        new Comment { Id = 5, Content = "This is the 5th comment's content.", PostId = 1 },
                        new Comment { Id = 6, Content = "This is the 6th comment's content.", PostId = 1 },
                        new Comment { Id = 7, Content = "This is the 7th comment's content.", PostId = 1 },
                    }
                },
                new BlogPost { Id = 2, Title = "Engineering Interview", Summary = "Once upon a time...", Content = "This is the content..." },
            };

        [HttpGet, Route("posts/{id}/comments")]
        public IHttpActionResult GetComments(int id, int take = 2, int skip = 2) {
            var comments = _posts.First(x => x.Id == id).Comments.Skip(skip).Take(take);
            return Ok(comments);
        }

        [HttpGet, Route("posts/{id}/comments/{commentId}")]
        public IHttpActionResult GetComment(int id, int commentId) {
            var comment = _posts.Single(x => x.Id == id).Comments?.SingleOrDefault(x => x.Id == commentId);
            if (comment == null)
                return NotFound();
            return Ok(comment);
        }

        [HttpPost, Route("posts/{id}/comments")]
        public IHttpActionResult AddComment(Comment comment, [FromUri] int id) {
            var post = _posts.First(x => x.Id == id);
            comment.Id = post.Comments.Max(x => x.Id) + 1;
            comment.PostId = id;
            post.Comments.Add(comment);
            return Ok(comment);
        }

        [HttpPut, Route("posts/{id}/comments/{commentId}")]
        public IHttpActionResult UpdateComment(Comment comment, [FromUri] int id, int commentId) {
            var post = _posts.First(x => x.Id == id);
            var c = post.Comments.Single(x => x.Id == commentId);
            c.Content += comment.Content;
            return Ok(c);
        }

        [HttpDelete, Route("comments/{id}")]
        public HttpResponseMessage AddComment(int id) {
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
    }

    public class BlogPost {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public IEnumerable<int> RelatedPosts { get; set; }
        public List<Comment> Comments { get; set; }
    }

    public class Comment {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string Content { get; set; }
    }

    public static class CacheExtensions {
        // From http://stackoverflow.com/questions/445050/how-can-i-cache-objects-in-asp-net-mvc
        public static T GetOrStore<T>(this System.Web.Caching.Cache cache, string key, TimeSpan expiration, Func<T> generator) {
            var result = cache.Get(key);

            if (result == null) {
                result = generator();
                cache.Insert(
                    key: key,
                    value: result,
                    dependencies: null,
                    absoluteExpiration: System.Web.Caching.Cache.NoAbsoluteExpiration,
                    slidingExpiration: expiration
                );
            }
            return (T)result;
        }
    }
}