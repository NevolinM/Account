namespace Account.Models
{
    public class StudentMatrix
    {
        public Student Student { get; set; } = default!;
        public Dictionary<int, int?> Scores { get; set; } = new();
    }
}
