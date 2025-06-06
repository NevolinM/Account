using System.ComponentModel.DataAnnotations;

namespace Account.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }

        // FK на дисциплину
        [Required]
        public int DisciplinId { get; set; }
        public Disciplin Disciplin { get; set; }

        // FK на группу студентов
        [Required]
        public int StudentGroupId { get; set; }
        public Student_Groups StudentGroup { get; set; }
    }
}
