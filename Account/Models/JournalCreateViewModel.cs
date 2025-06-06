using System.ComponentModel.DataAnnotations;

namespace Account.Models
{
    public class JournalCreateViewModel
    {
        [Required]
        [Range(1, 35, ErrorMessage = "Количество занятий должно быть от 1 до 35")]
        [Display(Name = "Количество занятий")]
        public int SessionCount { get; set; }

        public int DisciplinId { get; set; }
    }
}
