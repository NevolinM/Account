using System.ComponentModel.DataAnnotations;

namespace Account.Models
{
    public class ProfileViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Фамилия")]
        public string Surname { get; set; }

        [Display(Name = "Имя")]
        public string Name { get; set; }

        [Display(Name = "Отчество")]
        public string Patronymic { get; set; }

        [Display(Name = "Логин")]
        public string Login { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Телефон")]
        public string Phone { get; set; }

        [Display(Name = "Роль")]
        public string Role { get; set; } = default!;

        // Путь к существующей фотографии
        public string? PhotoUrl { get; set; }

        // Для загрузки новой
        [Display(Name = "Фото профиля")]
        public IFormFile? Photo { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
