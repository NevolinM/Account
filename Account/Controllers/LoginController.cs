using Account.Data;
using Account.Models;
using Account.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Account.Controllers
{
    public class LoginController : Controller
    {
        private readonly appdbcontext _context;
        private readonly ITeacherService _teacherService;
        private readonly byte[] _key;

        public LoginController(appdbcontext context, IConfiguration config, ITeacherService teacherService)
        {
            _context = context;
            _teacherService = teacherService; 
            _key = Encoding.UTF8.GetBytes(config["JwtSettings:SecretKey"]);
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string login, string password)
        {
            if (!ModelState.IsValid) return View();

            var token = await _teacherService.LoginAsync(login, password);
            if (token == null)
            {
                ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
                return View();
            }

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var identity = new ClaimsIdentity(
                jwt.Claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                });

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Проверка существования логина
            if (await _context.Teacher.AnyAsync(t => t.Login == model.Login))
            {
                ModelState.AddModelError(nameof(model.Login), "Логин занят");
                return View(model);
            }

            var teacher = new Teacher
            {
                Surname = model.Surname,
                Name = model.Name,
                Patronymic = model.Patronymic,
                Login = model.Login,
                Password = model.Password,
                Email = model.Email,
                Phone = model.Phone,
                Role = model.Role
            };

            await _teacherService.RegisterAsync(teacher);
            return RedirectToAction("Index", "Login");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
