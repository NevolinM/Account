using System.ComponentModel.DataAnnotations;

namespace Account.Models
{
    public class Disciplin
    {
        private string description;

        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Введите название")]
        public required string Name { get; set; }
        [StringLength(100, ErrorMessage = "Текст не более 100 символов")]
        [Required(ErrorMessage = "Введите описание")]
        public required string Description {  get; set; }
    }
}
