using Account.Data;
using Account.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.Claims;

namespace Account.Controllers
{
    public class DisciplinController : Controller
    {
        private readonly ILogger<DisciplinController> _logger;
        private readonly appdbcontext _context;
        private readonly IWebHostEnvironment _env;

        public DisciplinController(ILogger<DisciplinController> logger, appdbcontext context, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _env = env;
        }

        // Получение ID текущего преподавателя из claims
        private int? GetTeacherId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        public IActionResult Create_Disciplin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create_Disciplin_Test(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
            {
                ModelState.AddModelError(string.Empty, "Название и описание обязательны");
                return View("Create_Disciplin");
            }

            // Создаем и сохраняем новую дисциплину
            var discipline = new Disciplin { Name = name, Description = description };
            _context.Disciplin.Add(discipline);
            await _context.SaveChangesAsync();

            // Привязываем дисциплину к текущему преподавателю
            var teacherId = GetTeacherId();
            if (teacherId.HasValue)
            {
                _context.TeacherDisciplins.Add(new TeacherDisciplin
                {
                    TeacherId = teacherId.Value,
                    DisciplinId = discipline.Id
                });
                await _context.SaveChangesAsync();
            }

            return Redirect("/Home/Disciplin");
        }

        [HttpGet]
        public async Task<IActionResult> Edit_Disciplin(string name)
        {
            if (string.IsNullOrEmpty(name))
                return BadRequest("Не задано название дисциплины");

            var teacherId = GetTeacherId();
            if (!teacherId.HasValue)
                return Unauthorized();

            // Проверяем доступ преподавателя и получаем дисциплину
            var discipline = await _context.TeacherDisciplins
                .Where(td => td.TeacherId == teacherId && td.Disciplin.Name == name)
                .Select(td => td.Disciplin)
                .FirstOrDefaultAsync();

            if (discipline == null)
                return NotFound("Дисциплина не найдена или нет доступа");

            return View(discipline);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit_Disciplin(int id, string name, string description)
        {
            var teacherId = GetTeacherId();
            if (!teacherId.HasValue)
                return Unauthorized();

            // Проверяем, что преподаватель привязан к дисциплине
            var discipline = await _context.TeacherDisciplins
                .Where(td => td.TeacherId == teacherId && td.DisciplinId == id)
                .Select(td => td.Disciplin)
                .FirstOrDefaultAsync();

            if (discipline == null)
                return NotFound("Дисциплина не найдена или нет доступа");

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
            {
                ModelState.AddModelError(string.Empty, "Название и описание обязательны");
                return View(discipline);
            }

            discipline.Name = name;
            discipline.Description = description;
            await _context.SaveChangesAsync();

            // Закрываем окно и обновляем список
            var script = @"<script>window.opener.location.reload();window.close();</script>";
            return Content(script, "text/html");
        }

        [HttpGet]
        public async Task<IActionResult> Feed(int id)
        {
            var disciplin = await _context.Disciplin
                .FirstOrDefaultAsync(d => d.Id == id);
            if (disciplin == null) return NotFound();

            var materials = await _context.DisciplinMaterials
                .Where(m => m.DisciplinId == id)
                .Include(m => m.Comments)
                    .ThenInclude(c => c.Teacher)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();

            var vm = new DisciplinFeedViewModel
            {
                Disciplin = disciplin,
                Materials = materials
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMaterial(int disciplinId, string title, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Файл не выбран");
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("title", "Заголовок обязателен");
            }

            if (!ModelState.IsValid)
            {
                // Если валидация не прошла — показать ту же ленту
                return await Feed(disciplinId);
            }

            // Папка для хранения: wwwroot/materials/disc_{disciplinId}
            var uploadsRoot = Path.Combine(_env.WebRootPath, "materials", $"disc_{disciplinId}");
            Directory.CreateDirectory(uploadsRoot);

            var fullPath = Path.Combine(uploadsRoot, file.FileName);

            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            // Относительный URL для доступа из браузера
            var relativeUrl = $"/materials/disc_{disciplinId}/{file.FileName}";

            var material = new DisciplinMaterial
            {
                DisciplinId = disciplinId,
                Title = title,
                FileUrl = relativeUrl,
                UploadedAt = DateTime.UtcNow.AddHours(3)
            };
            _context.DisciplinMaterials.Add(material);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Feed), new { id = disciplinId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadMaterial(int id)
        {
            // 1) Находим запись в БД
            var material = await _context.DisciplinMaterials
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null)
                return NotFound();

            // 2) Строим полный путь на диске
            // material.FileUrl у вас хранится как "/materials/disc_1/abcd.pdf"
            var relativePath = material.FileUrl.TrimStart('/');
            var absolutePath = Path.Combine(_env.WebRootPath, relativePath);

            if (!System.IO.File.Exists(absolutePath))
                return NotFound();

            // 3) Определяем mime-тип по расширению
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(absolutePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            // 4) Отдаём файл с заголовками для скачивания
            var fileName = Path.GetFileName(absolutePath);
            return PhysicalFile(absolutePath, contentType, fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMaterial(int materialId)
        {
            var material = await _context.DisciplinMaterials
                .FirstOrDefaultAsync(m => m.Id == materialId);
            if (material == null)
                return NotFound();

            var comments = _context.DisciplinComments
                .Where(c => c.MaterialId == materialId);
            _context.DisciplinComments.RemoveRange(comments);

            // Удаляем файл с диска
            var relativePath = material.FileUrl.TrimStart('/');
            var absolutePath = Path.Combine(_env.WebRootPath, relativePath);
            if (System.IO.File.Exists(absolutePath))
                System.IO.File.Delete(absolutePath);

            var disciplinId = material.DisciplinId;
            _context.DisciplinMaterials.Remove(material);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Feed), new { id = disciplinId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostComment(int materialId, string text)
        {
            var material = await _context.DisciplinMaterials
                .FirstOrDefaultAsync(m => m.Id == materialId);
            if (material == null) return NotFound();

            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claimValue, out var teacherId)) return Unauthorized();

            var comment = new DisciplinComment
            {
                MaterialId = materialId,
                TeacherId = teacherId,
                Text = text,
                PostedAt = DateTime.UtcNow.AddHours(3)
            };
            _context.DisciplinComments.Add(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Feed), new { id = material.DisciplinId });
        }

        [HttpGet]
        public async Task<IActionResult> Tasks(int id)
        {
            // Находим дисциплину
            var discipline = await _context.Disciplin.FindAsync(id);
            if (discipline == null) return NotFound();

            // Получаем все задания (TaskAssignment) для этой дисциплины
            var tasks = await _context.TaskAssignments
                .Where(t => t.DisciplinId == id)
                .OrderByDescending(t => t.UploadedAt)
                .ToListAsync();

            var vm = new DisciplinFeedViewModel
            {
                Disciplin = discipline,
                Materials = new List<DisciplinMaterial>()
            };
            ViewData["TasksList"] = tasks;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTask(int disciplinId, string title, string? description, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Файл не выбран");
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("title", "Заголовок обязателен");
            }
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Tasks", new { id = disciplinId });
            }

            var tasksRoot = Path.Combine(_env.WebRootPath, "tasks", $"disc_{disciplinId}");
            Directory.CreateDirectory(tasksRoot);

            var fullPath = Path.Combine(tasksRoot, file.FileName);
            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }
            var relativeUrl = $"/tasks/disc_{disciplinId}/{file.FileName}";

            var task = new TaskAssignment
            {
                DisciplinId = disciplinId,
                Title = title,
                Description = description,
                FileUrl = relativeUrl,
                UploadedAt = DateTime.UtcNow.AddHours(3)
            };
            _context.TaskAssignments.Add(task);
            await _context.SaveChangesAsync();

            // Создаем папки для каждого студента и поддиректорию с заданием
            // Найдем всех студентов, принадлежащих к курсам данной дисциплины
            var studentIds = await _context.Courses
                .Where(c => c.DisciplinId == disciplinId)
                .Select(c => c.StudentGroupId)
                .Distinct()
                .ToListAsync();

            if (studentIds.Any())
            {
                var students = await _context.Students
                    .Where(s => studentIds.Contains(s.GroupsID))
                    .ToListAsync();

                foreach (var student in students)
                {
                    var studentDir = Path.Combine(_env.WebRootPath, "submissions", $"disc_{disciplinId}", $"student_{student.Id}");
                    Directory.CreateDirectory(studentDir);
                    var taskDir = Path.Combine(studentDir, $"task_{task.Id}");
                    Directory.CreateDirectory(taskDir);
                }
            }

            return RedirectToAction(nameof(Tasks), new { id = disciplinId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var task = await _context.TaskAssignments.FindAsync(taskId);
            if (task == null) return NotFound();

            var disciplinId = task.DisciplinId;

            // Удаляем файл
            var relativePath = task.FileUrl.TrimStart('/');
            var absolutePath = Path.Combine(_env.WebRootPath, relativePath);
            if (System.IO.File.Exists(absolutePath))
                System.IO.File.Delete(absolutePath);

            // Удаляем папки заданий у студентов
            var studentIds = await _context.Courses
                .Where(c => c.DisciplinId == disciplinId)
                .Select(c => c.StudentGroupId)
                .Distinct()
                .ToListAsync();

            if (studentIds.Any())
            {
                var students = await _context.Students
                    .Where(s => studentIds.Contains(s.GroupsID))
                    .ToListAsync();

                foreach (var student in students)
                {
                    var taskDir = Path.Combine(_env.WebRootPath, "submissions", $"disc_{disciplinId}", $"student_{student.Id}", $"task_{task.Id}");
                    if (Directory.Exists(taskDir))
                        Directory.Delete(taskDir, true);
                }
            }
            _context.TaskAssignments.Remove(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Tasks), new { id = disciplinId });
        }

        [HttpGet]
        public async Task<IActionResult> Report(int id)
        {
            // 1) Проверяем, что дисциплина существует
            var discipline = await _context.Disciplin.FindAsync(id);
            if (discipline == null)
                return NotFound();

            // 2) Берём все задания (TaskAssignment) для этой дисциплины
            var tasks = await _context.TaskAssignments
                .Where(t => t.DisciplinId == id)
                .OrderByDescending(t => t.UploadedAt)
                .ToListAsync();

            // 3) Получаем список групп (Student_Groups), которые участвуют в курсах этой дисциплины
            var groupIds = await _context.Courses
                .Where(c => c.DisciplinId == id)
                .Select(c => c.StudentGroupId)
                .Distinct()
                .ToListAsync();

            var groups = await _context.StudentGroups
                .Where(g => groupIds.Contains(g.Id))
                .OrderBy(g => g.Name)
                .ToListAsync();

            // 4) Загружаем все оценки (TaskGrade) для заданий этой дисциплины
            //    (включает информацию о студенте через StudentId и AssignmentId)
            //    Сначала найдём все ID-заданий текущей дисциплины
            var taskIds = tasks.Select(t => t.Id).ToList();

            var grades = await _context.TaskGrades
                .Where(g => taskIds.Contains(g.AssignmentId))
                .ToListAsync();

            // 5) Формируем итоговую модель DisciplinMatrixViewModel
            var model = new DisciplinMatrixViewModel
            {
                Disciplin = discipline,
                Tasks = tasks
            };

            foreach (var group in groups)
            {
                var groupMatrix = new GroupMatrix
                {
                    Group = group
                };

                // 6) Получаем всех студентов этой группы, отсортированных по фамилии/имени
                var students = await _context.Students
                    .Where(s => s.GroupsID == group.Id)
                    .OrderBy(s => s.Surname)
                    .ThenBy(s => s.Name)
                    .ToListAsync();

                foreach (var student in students)
                {
                    var studentMatrix = new StudentMatrix
                    {
                        Student = student
                    };

                    // 7) Для каждого задания заполняем оценку из TaskGrade (если есть) или null
                    foreach (var task in tasks)
                    {
                        var grade = grades
                            .FirstOrDefault(g => g.AssignmentId == task.Id && g.StudentId == student.Id);

                        if (grade != null)
                        {
                            studentMatrix.Scores[task.Id] = grade.Score;
                        }
                        else
                        {
                            studentMatrix.Scores[task.Id] = null;
                        }
                    }

                    groupMatrix.Students.Add(studentMatrix);
                }

                model.Groups.Add(groupMatrix);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Submissions(int id)
        {
            var discipline = await _context.Disciplin.FindAsync(id);
            if (discipline == null)
                return NotFound();

            // 2) Находим все StudentGroupId, которые участвуют в этой дисциплине через таблицу Courses
            var groupIds = await _context.Courses
                .Where(c => c.DisciplinId == id)
                .Select(c => c.StudentGroupId)
                .Distinct()
                .ToListAsync();

            // 3) Загружаем сами группы (Student_Groups) по этим ИД-шникам (и сортируем по имени группы)
            var groups = await _context.StudentGroups
                .Where(g => groupIds.Contains(g.Id))
                .OrderBy(g => g.Name)
                .ToListAsync();

            // 4) Для каждой группы загружаем студентов, сортируем по фамилии/имени
            var viewModel = new DisciplinSubmissionsViewModel
            {
                Disciplin = discipline
            };

            foreach (var grp in groups)
            {
                var studentsInGroup = await _context.Students
                    .Where(s => s.GroupsID == grp.Id)
                    .OrderBy(s => s.Surname)
                    .ThenBy(s => s.Name)
                    .ToListAsync();

                var groupVm = new StudentGroupViewModel
                {
                    Group = grp,
                    Students = studentsInGroup
                };

                viewModel.GroupedStudents.Add(groupVm);
            }

            // 5) Отдаем модель в представление
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> SubmissionsByStudent(int id, int studentId)
        {
            // Проверяем дисциплину и студента
            var discipline = await _context.Disciplin.FindAsync(id);
            if (discipline == null) return NotFound();

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Из TaskAssignments выбираем только те задания, которые относятся к этой дисциплине
            var tasks = await _context.TaskAssignments
                .Where(t => t.DisciplinId == id)
                .OrderByDescending(t => t.UploadedAt)
                .ToListAsync();

            // Получаем модель-ViewData для передачи discipline и student
            ViewData["Disciplin"] = discipline;
            ViewData["Student"] = student;
            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> SubmissionsFiles(int id, int studentId, int taskId)
        {
            // Проверяем, что дисциплина, студент и задание существуют
            var discipline = _context.Disciplin.Find(id);
            if (discipline == null) return NotFound();

            var student = _context.Students.Find(studentId);
            if (student == null) return NotFound();

            var task = _context.TaskAssignments.Find(taskId);
            if (task == null) return NotFound();

            // Формируем путь: wwwroot/submissions/disc_{id}/student_{studentId}/task_{taskId}
            var submissionsRoot = Path.Combine(_env.WebRootPath, "submissions", $"disc_{id}", $"student_{studentId}", $"task_{taskId}");

            List<string> files;
            if (Directory.Exists(submissionsRoot))
            {
                files = Directory.GetFiles(submissionsRoot)
                                 .Select(Path.GetFileName)
                                 .ToList();
            }
            else
            {
                files = new List<string>();
            }

            var submissions = await _context.TaskSubmissions
              .Where(s => s.AssignmentId == taskId && s.StudentId == studentId)
              .ToListAsync();

            var grade = await _context.TaskGrades
                .FirstOrDefaultAsync(g => g.AssignmentId == taskId && g.StudentId == studentId);

            ViewData["Disciplin"] = discipline;
            ViewData["Student"] = student;
            ViewData["Task"] = task;
            ViewData["Files"] = files;
            ViewData["Submissions"] = submissions;
            ViewData["Grade"] = grade;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateSubmission(int assignmentId, int score, int disciplinId, int studentId)
        {
            var existing = await _context.TaskGrades
    .FirstOrDefaultAsync(g => g.AssignmentId == assignmentId && g.StudentId == studentId);

            if (existing == null)
            {
                // 1.1) Если записи нет — создаём новую
                var newGrade = new TaskGrade
                {
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    Score = score
                };
                _context.TaskGrades.Add(newGrade);
            }
            else
            {
                // 1.2) Если уже была оценка — обновляем её
                existing.Score = score;
                _context.TaskGrades.Update(existing);
            }

            await _context.SaveChangesAsync();

            // 2) Перенаправляем обратно в SubmissionsFiles, чтобы преподаватель снова увидел страницу
            return RedirectToAction(nameof(SubmissionsFiles), new
            {
                id = disciplinId,
                studentId = studentId,
                taskId = assignmentId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubmission(int submissionId, int disciplinId, int studentId, int taskId)
        {
            // 1) Находим запись в БД
            var submission = await _context.TaskSubmissions.FindAsync(submissionId);
            if (submission == null)
                return NotFound();

            // 2) Удаляем файл с диска (если он там есть)
            var relativePath = submission.FileUrl.TrimStart('/');
            var absolutePath = Path.Combine(_env.WebRootPath, relativePath);
            if (System.IO.File.Exists(absolutePath))
                System.IO.File.Delete(absolutePath);

            // 3) Удаляем запись из БД
            _context.TaskSubmissions.Remove(submission);
            await _context.SaveChangesAsync();

            // 4) Возвращаемся обратно на ту же страницу, где показываются файлы
            return RedirectToAction(
                nameof(SubmissionsFiles),
                new { id = disciplinId, studentId = studentId, taskId = taskId }
            );
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport(int id)
        {
            // 1) Проверяем, что дисциплина существует
            var discipline = await _context.Disciplin.FindAsync(id);
            if (discipline == null)
                return NotFound();

            // 2) Берём все задания для этой дисциплины
            var tasks = await _context.TaskAssignments
                .Where(t => t.DisciplinId == id)
                .OrderByDescending(t => t.UploadedAt)
                .ToListAsync();

            // 3) Находим группы, связанные с этой дисциплиной
            var groupIds = await _context.Courses
                .Where(c => c.DisciplinId == id)
                .Select(c => c.StudentGroupId)
                .Distinct()
                .ToListAsync();

            var groups = await _context.StudentGroups
                .Where(g => groupIds.Contains(g.Id))
                .OrderBy(g => g.Name)
                .ToListAsync();

            // 4) Загружаем все оценки (TaskGrade) для заданий этой дисциплины
            var taskIds = tasks.Select(t => t.Id).ToList();
            var grades = await _context.TaskGrades
                .Where(g => taskIds.Contains(g.AssignmentId))
                .ToListAsync();

            // === Настраиваем EPPlus для некоммерческого использования ===
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Отчет");

            // 5) Заголовок документа (строка 1)
            sheet.Cells[1, 1].Value = $"Отчет по успеваемости — {discipline.Name}";
            sheet.Cells[1, 1, 1, tasks.Count + 2].Merge = true;
            sheet.Cells[1, 1].Style.Font.Bold = true;
            sheet.Cells[1, 1].Style.Font.Size = 14;
            sheet.Row(1).Height = 24;

            // 6) Шапка таблицы (строка 3)
            int headerRow = 3;
            sheet.Cells[headerRow, 1].Value = "Группа / Студент";
            sheet.Cells[headerRow, 1].Style.Font.Bold = true;
            sheet.Column(1).Width = 30;

            for (int j = 0; j < tasks.Count; j++)
            {
                var task = tasks[j];
                var cell = sheet.Cells[headerRow, j + 2];
                cell.Value = task.Title;
                cell.Style.Font.Bold = true;
                cell.Style.WrapText = true;
                sheet.Column(j + 2).Width = 20;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            // 7) Заполняем строки данными (начиная со строки 4)
            int currentRow = headerRow + 1;
            foreach (var group in groups)
            {
                // 7.1) Строка-разделитель с названием группы
                sheet.Cells[currentRow, 1].Value = $"Группа: {group.Name}";
                sheet.Cells[currentRow, 1, currentRow, tasks.Count + 1].Merge = true;
                sheet.Row(currentRow).Style.Font.Bold = true;
                sheet.Row(currentRow).Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Row(currentRow).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                currentRow++;

                // 7.2) Студенты этой группы
                var studentsInGroup = await _context.Students
                    .Where(s => s.GroupsID == group.Id)
                    .OrderBy(s => s.Surname).ThenBy(s => s.Name)
                    .ToListAsync();

                if (!studentsInGroup.Any())
                {
                    // Если нет студентов, выводим заметку
                    sheet.Cells[currentRow, 1].Value = "(студенты не найдены)";
                    sheet.Cells[currentRow, 1, currentRow, tasks.Count + 1].Merge = true;
                    sheet.Row(currentRow).Style.Font.Italic = true;
                    currentRow++;
                    continue;
                }

                foreach (var student in studentsInGroup)
                {
                    // ФИО студента в первом столбце
                    sheet.Cells[currentRow, 1].Value =
                        $"{student.Surname} {student.Name}" +
                        (string.IsNullOrWhiteSpace(student.Patronymic) ? "" : $" {student.Patronymic}");

                    // Для каждого задания заполняем ячейку оценкой из TaskGrade или «-»
                    for (int j = 0; j < tasks.Count; j++)
                    {
                        var task = tasks[j];
                        var grade = grades
                            .FirstOrDefault(g => g.AssignmentId == task.Id && g.StudentId == student.Id);

                        var cell = sheet.Cells[currentRow, j + 2];
                        if (grade != null)
                        {
                            cell.Value = grade.Score;
                        }
                        else
                        {
                            cell.Value = "-";
                            cell.Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                        }
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    currentRow++;
                }
            }

            // 8) Форматируем границы таблицы
            var tblRange = sheet.Cells[headerRow, 1, currentRow - 1, tasks.Count + 1];
            tblRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            tblRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            tblRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            tblRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // 9) Закрепляем шапку
            sheet.View.FreezePanes(headerRow + 1, 2);

            // 10) Готовим результат (поток в память) и возвращаем файл
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Успеваемость_{discipline.Name}_{DateTime.UtcNow:ddMMMyyyy}.xlsx";
            return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSubmission(int disciplinId, int studentId, int taskId, IFormFile file)
        {
            // 1) Простая валидация
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Файл не выбран");
            }
            if (!ModelState.IsValid)
            {
                // В случае ошибки возвращаем на страницу «SubmissionsFiles»
                return RedirectToAction(nameof(SubmissionsFiles), new { id = disciplinId, studentId, taskId });
            }

            // 2) Убедимся, что папка существует (по метке: wwwroot/submissions/disc_{disciplinId}/student_{studentId}/task_{taskId})
            var submissionDir = Path.Combine(
                _env.WebRootPath,
                "submissions",
                $"disc_{disciplinId}",
                $"student_{studentId}",
                $"task_{taskId}"
            );
            Directory.CreateDirectory(submissionDir);

            var existingPath = Path.Combine(submissionDir, file.FileName);
            if (System.IO.File.Exists(existingPath))
            {
                TempData["UploadError"] = $"Файл «{file.FileName}» уже загружен.";
                return RedirectToAction(nameof(SubmissionsFiles), new { id = disciplinId, studentId, taskId });
            }

            // 3) Сохраняем файл в эту папку
            using (var fs = new FileStream(existingPath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }
            // Формируем относительный URL, чтобы по нему можно было скачать:
            var relativeUrl = $"/submissions/disc_{disciplinId}/student_{studentId}/task_{taskId}/{file.FileName}";

            // 4) Записываем запись в таблицу TaskSubmission
            var submission = new TaskSubmission
            {
                AssignmentId = taskId,
                StudentId = studentId,
                SubmittedAt = DateTime.UtcNow,
                FileUrl = relativeUrl
            };
            _context.TaskSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            // 5) Перенаправляем обратно на страницу просмотра файлов этого задания
            return RedirectToAction(nameof(SubmissionsFiles), new { id = disciplinId, studentId, taskId });
        }

        [HttpPost]
        public IActionResult Check(Constant disciplin)
        {
            if (ModelState.IsValid)
            {
                return Redirect("/");
            }
            return View("Create_Disciplin");
        }
    }
}
