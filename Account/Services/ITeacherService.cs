using Account.Models;

namespace Account.Services
{
    public interface ITeacherService
    {
        Task RegisterAsync(Teacher model);
        Task<string?> LoginAsync(string login, string password);
        Task<Teacher?> GetByIdAsync(int id);
        Task UpdateAsync(Teacher teacher);
        Task<string?> SaveProfilePhotoAsync(int teacherId, IFormFile photo);
    }
}
