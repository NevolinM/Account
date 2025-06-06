namespace Account.Models
{
    public class JournalViewModel
    {
        public int DisciplinId { get; set; }
        public string DisciplinName { get; set; } = default!;
        public int JournalId { get; set; }        
        public int SessionCount { get; set; }     
        public List<JournalRowViewModel> Rows { get; set; } = new();
    }
}
