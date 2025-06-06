using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Models
{
    public class DisciplinComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(DisciplinMaterial))]
        public int MaterialId { get; set; }
        public DisciplinMaterial DisciplinMaterial { get; set; } = default!;

        [Required]
        [ForeignKey(nameof(Teacher))]
        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; } = default!;

        [Required, StringLength(1000)]
        public string Text { get; set; } = default!;

        [Required]
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    }
}
