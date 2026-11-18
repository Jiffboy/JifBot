using JifBot.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace JifBot.Interfaces
{
    public class Rule34Interface
    {
        async public Task<R34Post> GetComments(string tags)
        {
            var db = new BotBaseContext();
            var key = db.Variable.AsQueryable().Where(v => v.Name == "r34Key").First().Value;
            var id = db.Variable.AsQueryable().Where(v => v.Name == "r34Id").First().Value;
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync($"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&api_key={key}&user_id={id}&limit=500&tags=-ai_generated sort:random {tags}");

                string xmlResponse = await response.Content.ReadAsStringAsync();
                XmlSerializer postSerializer = new XmlSerializer(typeof(PostResponse));
                StringReader postReader = new StringReader(xmlResponse);
                PostResponse postResponse = (PostResponse)postSerializer.Deserialize(postReader);

                var eligiblePosts = postResponse.Posts.Where(e => e.HasComments == "true").ToList();

                Random rnd = new Random();
                while (eligiblePosts.Count > 0)
                {
                    var index = rnd.Next(eligiblePosts.Count);
                    var post = eligiblePosts[index];
                    response = await client.GetAsync($"https://api.rule34.xxx/index.php?page=dapi&s=comment&q=index&api_key={key}&user_id={id}&post_id={post.Id}");
                    xmlResponse = await response.Content.ReadAsStringAsync();
                    XmlSerializer commentSerializer = new XmlSerializer(typeof(CommentResponse));
                    StringReader commentReader = new StringReader(xmlResponse);
                    CommentResponse commentResponse = (CommentResponse)commentSerializer.Deserialize(commentReader);
                    if (commentResponse.Comments.Count > 0)
                    {
                        return new R34Post { Post = post, Comments = commentResponse.Comments };
                    }
                    else
                    {
                        eligiblePosts.RemoveAt(index);
                    }
                }

                return new R34Post { Post = new Post(), Comments = new List<Comment>() };
            }
        }
    }

    public class R34Post 
    {
        public Post Post { get; set; }
        public List<Comment> Comments { get; set; }
    }

    [XmlRoot("posts")]
    public class PostResponse
    {
        [XmlElement("post")]
        public List<Post> Posts { get; set; }
    }

    public class Post
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("sample_url")]
        public string ThumbnailUrl { get; set; }

        [XmlAttribute("tags")]
        public string Tags { get; set; }

        [XmlAttribute("has_comments")]
        public string HasComments { get; set; }

        [XmlAttribute("rating")]
        public string Rating { get; set; }
    }

    [XmlRoot("comments")]
    public class CommentResponse
    {
        [XmlElement("comment")]
        public List<Comment> Comments { get; set; }
    }


    public class Comment
    {
        [XmlAttribute("creator")]
        public string Creator { get; set; }

        [XmlAttribute("body")]
        public string Body { get; set; }
    }
}
