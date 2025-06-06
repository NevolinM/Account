namespace Account.Models
{
    public class TaskFolderViewModel
    {
        /// Модель для одной папки задания внутри папки студента:
        /// - само задание (TaskAssignment)
        /// - список файлов, которые студент туда загрузил
        public TaskAssignment Task { get; set; } = default!;
        public List<string> UploadedFiles { get; set; } = new();
    }
}
