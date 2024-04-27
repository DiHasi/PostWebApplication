using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PostApplication.Context;
using PostApplication.Contracts;
using PostApplication.Models;
using PostApplication.Utilities;
using X.PagedList;


namespace PostApplication.Controllers
{
    public class PostController(PostsContext context, IHttpContextAccessor httpContextAccessor) : Controller
    {
        // GET: Post
        public async Task<IActionResult> Index(string searchString, int? categoryFilter, int? page)
        {
            var pageNumber = page ?? 1;
            const int pageSize = 3;

            var posts = context.Posts
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .OrderByDescending(p => p.Id).AsQueryable();


            if (!string.IsNullOrEmpty(searchString))
            {
                var searchStringLower = searchString.ToLower();

                posts = posts.Where(p =>
                    p.Name.ToLower().Contains(searchStringLower) ||
                    p.Description.ToLower().Contains(searchStringLower));
            }

            if (categoryFilter.HasValue)
            {
                posts = posts.Where(p => p.Category != null && p.Category.Id == categoryFilter.Value);
            }

            var pagedPosts = await posts.ToPagedListAsync(pageNumber, pageSize);

            ViewBag.CurrentFilter = searchString;
            if (categoryFilter != null) ViewBag.CurrentCategory = categoryFilter;
            ViewBag.Categories = await context.Categories.ToListAsync();

            return View(pagedPosts);
        }

        // GET: Post/Details/5
        [Route("Post/Details/{slug}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Details(string slug)
        {
            var post = await context.Posts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(m => m.Slug == slug);
            if (post == null)
            {
                return NotFound();
            }

            // post.NewComment = new Comment();

            return View(post);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int id, string? comment)
        {
            var originalPost = await context.Posts.FindAsync(id);
            if (originalPost == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(comment))
            {
                TempData["Message"] = $"Введите не пустой коментарий";
                TempData["MessageType"] = "danger";
                return RedirectToAction("Details", new { id = originalPost.Id });
            }
            
            originalPost.Comments.Add(new Comment{
                Author = httpContextAccessor.HttpContext?.User.Identity?.Name, 
                Body = comment});
            await context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = originalPost.Id });
        }

        // GET: Post/Create
        [Authorize]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(context.Categories, "Id", "Name");
            ViewBag.Tags = new MultiSelectList(context.Tags, "Id", "Name");
            return View();
        }

        // POST: Post/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(PostDTO postDto)
        {
            ViewBag.Categories = new SelectList(context.Categories, "Id", "Name");
            ViewBag.Tags = new MultiSelectList(context.Tags, "Id", "Name");
            if (!ModelState.IsValid) return View(postDto);

            var post = new Post
            {
                Name = postDto.Name,
                Description = postDto.Description,
                Category = await context.Categories.FindAsync(postDto.CategoryId),
                Tags = await context.Tags.Where(t => postDto.TagIds != null && postDto.TagIds.Contains(t.Id))
                    .ToListAsync(),
                Author = httpContextAccessor.HttpContext?.User.Identity?.Name,
                Slug = SlugGenerator.Generate(postDto.Name)
            };

            var (error, fileName) = await ProcessFile(postDto.FeaturedImageFile);

            if (error != null)
            {
                TempData["Message"] = $"{error}";
                TempData["MessageType"] = "danger";
                return View(postDto);
            }

            post.FeaturedImage = fileName;

            context.Add(post);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Post/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await context.Posts
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var username = httpContextAccessor.HttpContext?.User.Identity?.Name;
            if (post.Author != username)
            {
                return Forbid();
            }

            var postDto = new PostDTO
            {
                Id = post.Id,
                Name = post.Name,
                Description = post.Description,
                CategoryId = post.Category?.Id,
                TagIds = post.Tags.Select(t => t.Id).ToList(),
            };

            ViewBag.Categories = new SelectList(context.Categories, "Id", "Name");
            ViewBag.Tags = new MultiSelectList(context.Tags, "Id", "Name");
            return View(postDto);
        }

        // POST: Post/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, PostDTO postDto)
        {
            if (id != postDto.Id)
            {
                return NotFound();
            }

            var username = httpContextAccessor.HttpContext?.User.Identity?.Name;
            if ((await context.Posts.FindAsync(id))?.Author != username)
            {
                return Forbid();
            }

            if (!ModelState.IsValid) return View(postDto);

            var post = await context.Posts
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);

            ViewBag.Categories = new SelectList(context.Categories, "Id", "Name");
            ViewBag.Tags = new MultiSelectList(context.Tags, "Id", "Name");

            if (post == null)
            {
                return View(postDto);
            }

            if (postDto.FeaturedImageFile != null)
            {
                var (error, fileName) = await ProcessFile(postDto.FeaturedImageFile);
                if (error == null)
                {
                    post.FeaturedImage = fileName ?? post.FeaturedImage;
                }
                else
                {
                    TempData["Message"] = $"{error}";
                    TempData["MessageType"] = "danger";
                    return View(postDto);
                }
            }

            post.Name = postDto.Name;
            post.Description = postDto.Description;
            post.Category = await context.Categories.FindAsync(postDto.CategoryId);
            post.Tags = await context.Tags.Where(t => postDto.TagIds != null && postDto.TagIds.Contains(t.Id))
                .ToListAsync();
            post.Slug = SlugGenerator.Generate(postDto.Name);

            context.Update(post);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Post/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            var post = await context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            
            var username = httpContextAccessor.HttpContext?.User.Identity?.Name;
            if (post.Author != username)
            {
                return Forbid();
            }

            return View(post);
        }

        // POST: Post/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await context.Posts
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Include(p => p.Comments)
                .FirstAsync(p => p.Id == id);

            var username = httpContextAccessor.HttpContext?.User.Identity?.Name;
            if (post?.Author != username)
            {
                return Forbid();
            }

            if (post != null) context.Posts.Remove(post);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<(string? Error, string? Value)> ProcessFile(IFormFile? file)
        {
            if (file == null) return (null, "default.jpg");
            var supportedTypes = new[] { "jpg", "jpeg", "png", "gif", "bmp" };
            var fileExt = Path.GetExtension(file.FileName)[1..];
            if (!supportedTypes.Contains(fileExt))
            {
                return ("Invalid file type", null);
            }

            var username = httpContextAccessor.HttpContext?.User.Identity?.Name;
            if (username == null) return ("User not found", null);

            var userFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", username);
            Directory.CreateDirectory(userFolderPath);

            var uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmssfff}.{fileExt}";

            var path = Path.Combine(userFolderPath, uniqueFileName);

            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return (null, Path.Combine(username, uniqueFileName));
        }
    }
}