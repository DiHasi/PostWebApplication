using System.ComponentModel.DataAnnotations.Schema;

namespace PostApplication.Models;

public class Article
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Characters { get; set; }
    public string FeaturedImage { get; set; }
}