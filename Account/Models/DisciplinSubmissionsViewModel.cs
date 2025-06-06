namespace Account.Models
{
    public class DisciplinSubmissionsViewModel
    {
        public Disciplin Disciplin { get; set; } = default!;
        public List<StudentGroupViewModel> GroupedStudents { get; set; } = new();
    }
}
