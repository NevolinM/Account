using System.ComponentModel.DataAnnotations;

namespace Account.Models
{
    public class RegisterViewModel
    {
        [Required] public string Surname { get; set; }
        [Required] public string Name { get; set; }
        public string Patronymic { get; set; }

        [Required][Display(Name = "Логин")] public string Login { get; set; }
        [Required][DataType(DataType.Password)] public string Password { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают.")]
        public string ConfirmPassword { get; set; }

        [EmailAddress] public string Email { get; set; }
        [Phone] public string Phone { get; set; }

        [Required]
        [Display(Name = "Роль")]
        public string Role { get; set; }
    }
}
