using Account.Data;
using Account.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NuGet.DependencyResolver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Account.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly appdbcontext _context;
        private readonly IPasswordHasher _hasher;
        private readonly byte[] _key;
        private readonly TimeSpan _tokenLifetime = TimeSpan.FromHours(2);
        private readonly IWebHostEnvironment _env;

        public TeacherService(appdbcontext context, IPasswordHasher hasher, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _hasher = hasher;
            _key = Encoding.UTF8.GetBytes(config["JwtSettings:SecretKey"]!);
            _env = env;
        }
        public async Task<Teacher?> GetByIdAsync(int id)
        {
            return await _context.Teacher.FindAsync(id);
        }

        public async Task RegisterAsync(Teacher model)
        {
            // Хешируем пароль и сохраняем
            model.Password = _hasher.Generate(model.Password);
            _context.Teacher.Add(model);
            await _context.SaveChangesAsync();
        }

        public async Task<string?> LoginAsync(string login, string password)
        {
            var user = await _context.Teacher
                .FirstOrDefaultAsync(u => u.Login == login);

            if (user == null || !_hasher.Verify(password, user.Password))
                return null;

            // Генерация JWT
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.Surname} {user.Name}"),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.Add(_tokenLifetime),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task UpdateAsync(Teacher teacher)
        {
            _context.Teacher.Update(teacher);
            await _context.SaveChangesAsync();
        }
        public async Task<string?> SaveProfilePhotoAsync(int teacherId, IFormFile photo)
        {
            if (photo == null || photo.Length == 0) return null;

            var uploads = Path.Combine(_env.WebRootPath, "images", "profiles");
            Directory.CreateDirectory(uploads);

            var filePath = Path.Combine(uploads, photo.FileName);

            using var fs = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(fs);

            return $"/images/profiles/{photo.FileName}";
        }

    }
}
