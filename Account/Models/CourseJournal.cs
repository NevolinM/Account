using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Models
{
    public class CourseJournal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Disciplin))]
        public int DisciplinId { get; set; }

        public Disciplin Disciplin { get; set; } = default!;

        [Required]
        [Range(1, 50, ErrorMessage = "Количество занятий должно быть от 1 до 50")]
        public int SessionCount { get; set; }

        public ICollection<CourseJournalEntry> Entries { get; set; } = new List<CourseJournalEntry>();
    }
}
