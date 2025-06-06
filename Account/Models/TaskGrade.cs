using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Models
{
    public class TaskGrade
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(TaskAssignment))]
        public int AssignmentId { get; set; }
        public TaskAssignment TaskAssignment { get; set; } = default!;

        [Required]
        [ForeignKey(nameof(Student))]
        public int StudentId { get; set; }
        public Student Student { get; set; } = default!;

        [Required]
        [Range(0, 100, ErrorMessage = "Оценка должна быть между 0 и 100")]
        public int Score { get; set; }
    }
}
