namespace Account.Models
{
    public class DisciplinReportViewModel
    {
        public Disciplin Disciplin { get; set; } = default!;

        public List<TaskAssignment>? Tasks { get; set; }

        public List<TaskSubmission> Submissions { get; set; } = new List<TaskSubmission>();
    }
}
