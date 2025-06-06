using System.ComponentModel.DataAnnotations;

namespace Account.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string Patronymic { get; set; }

        [Required(ErrorMessage = "Поле Логин обязательно для заполнения")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Поле Пароль  обязательно для заполнения")]
        public string Password { get; set; }

        public string? Photo { get; set; }

        [Required]
        public string Role { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
