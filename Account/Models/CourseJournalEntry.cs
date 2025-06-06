using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Models
{
    public class CourseJournalEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(CourseJournal))]
        public int JournalId { get; set; }

        public CourseJournal CourseJournal { get; set; } = default!;

        [Required]
        [ForeignKey(nameof(Student))]
        public int StudentId { get; set; }
        public Student Student { get; set; } = default!;

        [Required]
        [Range(1, 50)]
        public int SessionNumber { get; set; }

        // Например, «+», «н», «у» и т.д.
        [StringLength(3)]
        public string? Mark { get; set; }
    }
}
