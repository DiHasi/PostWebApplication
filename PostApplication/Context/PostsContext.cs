using Microsoft.EntityFrameworkCore;
using PostApplication.Models;


namespace PostApplication.Context;

public partial class PostsContext : DbContext
{
    public PostsContext()
    {
    }

    public PostsContext(DbContextOptions<PostsContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        // => optionsBuilder.UseNpgsql(@"");
        => optionsBuilder.UseSqlite(@"Data Source=app.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>()
            .HasMany(p => p.Tags)
            .WithMany(t => t.Posts)
            .UsingEntity(j => j.ToTable("PostTags"));
    }

    private static void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
    }
    
    public DbSet<Article> Articles { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
}
