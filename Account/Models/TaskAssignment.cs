using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Models
{
    public class TaskAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Disciplin))]
        public int DisciplinId { get; set; }
        public Disciplin Disciplin { get; set; } = default!;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string FileUrl { get; set; } = default!;

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
