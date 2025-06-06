namespace Account.Models
{
    public class CourseDetailsViewModel
    {
        public Course Course { get; set; } = default!;
        public List<Student> Students { get; set; } = new();
    }
}
