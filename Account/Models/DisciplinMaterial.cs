using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Models
{
    public class DisciplinMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Disciplin))]
        public int DisciplinId { get; set; }
        public Disciplin Disciplin { get; set; } = default!;

        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        [Required, StringLength(500)]
        public string FileUrl { get; set; } = default!;

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public List<DisciplinComment> Comments { get; set; } = new();
    }
}
