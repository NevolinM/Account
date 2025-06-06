using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Account.Models
{
    public class TeacherDisciplin
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Teacher))]
        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; } = default!;

        [ForeignKey(nameof(Disciplin))]
        public int DisciplinId { get; set; }
        public Disciplin Disciplin { get; set; } = default!;
    }
}
