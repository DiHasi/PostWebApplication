namespace PostApplication.Contracts;

public class PostDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public IFormFile? FeaturedImageFile { get; set; }
    public int? CategoryId { get; set; }
    public List<int>? TagIds { get; set; } = [];
    
}