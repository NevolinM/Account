namespace Account.Models
{
    public class JournalRowViewModel
    {
        public int StudentId { get; set; }
        public string StudentFullName { get; set; } = default!;
        public Dictionary<int, JournalCellViewModel> Cells { get; set; } = new();

    }
}
