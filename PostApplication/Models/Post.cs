using System.ComponentModel.DataAnnotations.Schema;

namespace PostApplication.Models;

public class Post
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string? FeaturedImage { get; set; }
    public string? Slug { get; set; }
    public string? Author { get; set; }
    public Category? Category { get; set; }
    public List<Tag> Tags { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
}