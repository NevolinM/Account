namespace Account.Models
{
    public class StudentFolderViewModel
    {
        /// Модель для одной папки студента:
        /// - информация о студенте
        /// - список заданий (таких, какие появились в файловой системе)
        public Student Student { get; set; } = default!;
        public List<TaskFolderViewModel> TaskFolders { get; set; } = new();
    }
}
