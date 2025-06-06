namespace Account.Models
{
    public class DisciplinFeedViewModel
    {
        public Disciplin Disciplin { get; set; } = default!;
        public List<DisciplinMaterial> Materials { get; set; } = new();
    }
}
