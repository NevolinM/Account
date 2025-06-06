using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Models
{
    public class TaskSubmission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(TaskAssignment))]
        public int AssignmentId { get; set; }
        public TaskAssignment Assignment { get; set; } = default!;

        [Required]
        [ForeignKey("Student")]
        public int StudentId { get; set; }
        public Student Student { get; set; } = default!;

        [Required]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        /// Путь к файлу решения
        [StringLength(500)]
        public string? FileUrl { get; set; }
    }
}
