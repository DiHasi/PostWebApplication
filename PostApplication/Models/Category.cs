using System.ComponentModel.DataAnnotations.Schema;

namespace PostApplication.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Slug { get; set; }
    
    public ICollection<Post>? Posts { get; set; }
}