using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostApplication.Context;
using PostApplication.Models;
using PostApplication.Utilities;

namespace PostApplication.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BlogController(PostsContext context, IConfiguration configuration) : ControllerBase
{
    public record PostResponse(
        int Id,
        string Name,
        string Description,
        string? Slug,
        List<Comment>? Comments,
        CategoryResponse? Category,
        List<TagResponse>? Tags);

    public record PostRequest(
        string Name,
        string Description,
        int? CategoryId,
        List<int>? TagIds);

    public record CategoryResponse(int Id, string Name, string? Slug);

    public record TagResponse(int Id, string Name);

    public record CommentResponse(int Id, string Body, string Author);

    public class UserRegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class UserLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // GET: api/Blog
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostResponse>>> GetPosts(string? include, string? search,
        string? category)
    {
        var postsQuery = context.Posts.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            postsQuery = postsQuery.Where(p => p.Name.ToLower().Contains(searchLower)
                                               || p.Description.ToLower().Contains(searchLower)
                                               || p.Slug!.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrEmpty(category))
        {
            postsQuery = postsQuery.Where(p => p.Category != null && (p.Category.Name == category
                                                                      || p.Category.Slug == category));
        }

        var posts = await postsQuery
            .Select(p => new PostResponse(
                p.Id,
                p.Name,
                p.Description,
                p.Slug,
                include != null && include.Equals("comments") ? p.Comments : new List<Comment>(),
                p.Category == null ? null : new CategoryResponse(p.Category.Id, p.Category.Name, p.Category.Slug),
                p.Tags.Select(t => new TagResponse(t.Id, t.Name)).ToList()
            ))
            .ToListAsync();

        return posts;
    }

    // GET: api/Blog/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostResponse>> GetPost(int id, string? include)
    {
        var post = await context.Posts
            .Include(p => p.Category)
            .Include(p => p.Tags)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
        {
            return NotFound();
        }

        var postResponse = new PostResponse(
            post.Id,
            post.Name,
            post.Description,
            post.Slug,
            include is "comments" ? post.Comments : [],
            post.Category == null
                ? null
                : new CategoryResponse(post.Category.Id, post.Category.Name, post.Category.Slug),
            post.Tags.Select(t => new TagResponse(t.Id, t.Name)).ToList()
        );

        return postResponse;
    }

    // GET: api/Blog/5/comments
    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<IEnumerable<CommentResponse>>> GetPostComments(int id)
    {
        var post = await context.Posts
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
        {
            return NotFound();
        }

        return post.Comments.Select(c => new CommentResponse(c.Id, c.Body!, c.Author!)).ToList();
    }

    // PUT: api/Blog/5
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<PostResponse>> PutPost(int id, PostRequest postRequest)
    {
        var post = await context.Posts
            .Include(p => p.Category)
            .Include(p => p.Tags)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (post == null)
        {
            return NotFound();
        }

        post.Name = postRequest.Name;
        post.Description = postRequest.Description;
        post.Category = postRequest.CategoryId != null ? await context.Categories.FindAsync(postRequest.CategoryId) : post.Category;
        post.Tags = postRequest.TagIds != null
            ? await context.Tags.Where(t => postRequest.TagIds.Contains(t.Id)).ToListAsync()
            : post.Tags;
        post.Slug = SlugGenerator.Generate(postRequest.Name);
            
        await context.SaveChangesAsync();
            
           
        var postResponse = new PostResponse(
            post.Id,
            post.Name,
            post.Description,
            post.Slug,
            post.Comments,
            post.Category == null ? null : new CategoryResponse(post.Category.Id, post.Category.Name, post.Category.Slug),
            post.Tags.Select(t => new TagResponse(t.Id, t.Name)).ToList()
        );

        return postResponse;
    }

    // POST: api/Blog
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PostResponse>> PostPost(PostRequest post)
    {
        var newPost = new Post
        {
            Name = post.Name,
            Description = post.Description,
            Category = post.CategoryId == null ? null : await context.Categories.FindAsync(post.CategoryId),
            Slug = SlugGenerator.Generate(post.Name),
            FeaturedImage = "default.jpg",
            Author = HttpContext.User.Identity!.Name,
            Tags = post.TagIds != null
                ? await context.Tags.Where(t => post.TagIds.Contains(t.Id)).ToListAsync()
                : []
        };

        var addedPost = context.Posts.Add(newPost).Entity;
        await context.SaveChangesAsync();

        var postResponse = new PostResponse(
            addedPost.Id,
            addedPost.Name,
            addedPost.Description,
            addedPost.Slug,
            addedPost.Comments,
            addedPost.Category == null ? null : new CategoryResponse(addedPost.Category.Id, addedPost.Category.Name, addedPost.Category.Slug),
            addedPost.Tags.Select(t => new TagResponse(t.Id, t.Name)).ToList()
        );

        return CreatedAtAction(nameof(GetPost), new { id = addedPost.Id }, postResponse);
    }

    // DELETE: api/Blog/5
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(int id)
    {
        var post = await context.Posts
            .Include(p => p.Category)
            .Include(p => p.Tags)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (post == null)
        {
            return NotFound();
        }

        context.Posts.Remove(post);
        await context.SaveChangesAsync();

        return Ok(new {Message = "Пост удален"});
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegisterRequest request)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (existingUser != null)
        {
            return BadRequest("Пользователь уже существует");
        }

        var newUser = new User
        {
            Username = request.Username,
            Password = request.Password,
        };

        context.Users.Add(newUser);
        await context.SaveChangesAsync();

        var token = JwtGenerator.Generate(newUser, configuration);

        return Ok(new { Token = token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
        {
            return BadRequest("Неправильное имя пользователя или пароль");
        }

        if (request.Password != user.Password)
        {
            return BadRequest("Неправильное имя пользователя или пароль");
        }

        var token = JwtGenerator.Generate(user, configuration);

        return Ok(new { Token = token });
    }
}