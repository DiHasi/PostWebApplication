using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PostApplication.Context;
using PostApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PostApplication.Utilities;

namespace PostApplication.Controllers
{
    public class AccountController(PostsContext context, IConfiguration configuration) : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(User user)
        {
            var dbUser = await context.Users.FirstOrDefaultAsync(u => u.Username == user.Username && u.Password == user.Password);

            if (dbUser == null)
            {
                TempData["Message"] = "Неверное имя пользователя или пароль";
                TempData["MessageType"] = "danger";
                return View(user);
            }

            var token = JwtGenerator.Generate(user, configuration);
            Response.Cookies.Append("jwt", token );
            return RedirectToAction("Profile", "Account");
        }
        
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (!ModelState.IsValid) return View(user);
            
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
            if (existingUser != null)
            {
                TempData["Message"] = "Пользователь уже существует";
                TempData["MessageType"] = "danger";
                return View(user);
            }
            
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var token = JwtGenerator.Generate(user, configuration);
            
            Response.Cookies.Append("jwt", token);
            return RedirectToAction("Profile", "Account");

        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Index", "Post");
        }
        public IActionResult Profile()
        {
            return View();
        }
    }
}