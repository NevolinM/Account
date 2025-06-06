namespace Account.Models
{
    public class DisciplinMatrixViewModel
    {
        public Disciplin Disciplin { get; set; } = default!;
        public List<TaskAssignment> Tasks { get; set; } = new();
        public List<GroupMatrix> Groups { get; set; } = new();
    }
}
