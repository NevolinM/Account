namespace Account.Models
{
    public class GroupMatrix
    {
        public Student_Groups Group { get; set; } = default!;
        public List<StudentMatrix> Students { get; set; } = new();
    }
}
