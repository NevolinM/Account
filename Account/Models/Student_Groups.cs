using System.ComponentModel.DataAnnotations;

namespace Account.Models
{
    public class Student_Groups
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        // навигация назад
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
