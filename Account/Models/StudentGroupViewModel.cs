namespace Account.Models
{
    public class StudentGroupViewModel
    {
        public Student_Groups Group { get; set; } = default!;
        public List<Student> Students { get; set; } = new();
    }
}
