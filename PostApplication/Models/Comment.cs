using System.ComponentModel.DataAnnotations.Schema;

namespace PostApplication.Models;

public class Comment
{
    public int Id { get; set; }
    public string? Body { get; set; }
    public string? Author { get; set; }
}