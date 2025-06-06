using System.ComponentModel.DataAnnotations;

namespace Account.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Логин")]
        public string Login { get; set; }

        [Required]
        [UIHint("password")]
        [Display(Name = "Пароль")]
        public string Password { get; set; }
    }
}
