using Account.Data;
using Account.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;       
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Collections.Generic;          
using System.Linq;
using System.Security.Claims;

namespace Account.Controllers
{
    [Authorize]
    public class JournalController : Controller
    {
        private readonly appdbcontext _context;

        public JournalController(appdbcontext context)
        {
            _context = context;
        }

        // Вспомогательный метод: получить id текущего преподавателя (необязательно, но можно для прав)
        private int? GetTeacherId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        // GET: /Journal/Index/{disciplinId}
        public async Task<IActionResult> Index(int id)
        {
            // id – это DisciplinId (Id дисциплины)
            var disciplin = await _context.Disciplin.FindAsync(id);
            if (disciplin == null)
                return NotFound();

            // Проверка, привязан ли текущий преподаватель к этой дисциплине (необязательно):
            var teacherId = GetTeacherId();
            if (teacherId == null)
                return Unauthorized();

            var hasAccess = await _context.TeacherDisciplins
                .AnyAsync(td => td.TeacherId == teacherId && td.DisciplinId == id);
            if (!hasAccess)
                return Forbid();

            // Есть ли уже журнал для этой дисциплины?
            var journal = await _context.CourseJournals
                .FirstOrDefaultAsync(j => j.DisciplinId == id);

            if (journal == null)
            {
                // Журнала ещё нет – отправляем View, где будет только кнопка «Создать»
                ViewData["DisciplinName"] = disciplin.Name;
                ViewData["DisciplinId"] = id;
                return View("IndexEmpty");
            }

            // Если журнал уже есть – строим данные для таблицы
            var students = await _context.Students
                .Where(s => _context.Courses
                    .Any(c => c.DisciplinId == id && c.StudentGroupId == s.GroupsID))
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .ToListAsync();

            // Загружаем все записи для этого журнала
            var entries = await _context.CourseJournalEntries
                .Where(e => e.JournalId == journal.Id)
                .ToListAsync();

            // Формируем ViewModel
            var vm = new JournalViewModel
            {
                DisciplinId = id,
                DisciplinName = disciplin.Name,
                JournalId = journal.Id,
                SessionCount = journal.SessionCount
            };

            int counter = 0;
            foreach (var s in students)
            {
                var row = new JournalRowViewModel
                {
                    StudentId = s.Id,
                    StudentFullName = $"{s.Surname} {s.Name}" + (string.IsNullOrWhiteSpace(s.Patronymic) ? "" : $" {s.Patronymic}")
                };

                // Инициализируем ячейки: если для данного студента и номера занятия есть запись, забираем Mark
                for (int i = 1; i <= journal.SessionCount; i++)
                {
                    var existing = entries
                        .FirstOrDefault(e => e.StudentId == s.Id && e.SessionNumber == i);

                    row.Cells[i] = new JournalCellViewModel
                    {
                        EntryId = existing?.Id ?? 0,
                        StudentId = s.Id,
                        SessionNumber = i,
                        Mark = existing?.Mark
                    };
                }

                vm.Rows.Add(row);
                counter++;
            }

            return View("Index", vm);
        }

        // GET: /Journal/Create/{disciplinId}
        [HttpGet]
        public IActionResult Create(int id)
        {
            // id – DisciplinId
            ViewData["DisciplinId"] = id;
            return View(new JournalCreateViewModel { DisciplinId = id });
        }

        // POST: /Journal/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var disciplin = await _context.Disciplin.FindAsync(model.DisciplinId);
            if (disciplin == null)
                return NotFound();

            // Проверка доступа преподавателя (как выше)
            var teacherId = GetTeacherId();
            if (teacherId == null)
                return Unauthorized();

            var hasAccess = await _context.TeacherDisciplins
                .AnyAsync(td => td.TeacherId == teacherId && td.DisciplinId == model.DisciplinId);
            if (!hasAccess)
                return Forbid();

            // Создаём CourseJournal
            var journal = new CourseJournal
            {
                DisciplinId = model.DisciplinId,
                SessionCount = model.SessionCount
            };
            _context.CourseJournals.Add(journal);
            await _context.SaveChangesAsync();

            // Отправляем на Index уже с заполненной таблицей
            //return RedirectToAction(nameof(Index), new { id = model.DisciplinId });
            var script = @"<script>
                               if (window.opener) {
                                   window.opener.location.reload();
                               }
                               window.close();
                           </script>";
            return Content(script, "text/html");
        }

        // POST: /Journal/Save/{journalId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int journalId, Dictionary<string, string> marks)
        {
            var journal = await _context.CourseJournals.FindAsync(journalId);
            if (journal == null)
                return NotFound();

            // Проверка доступа аналогично
            var teacherId = GetTeacherId();
            if (teacherId == null)
                return Unauthorized();
            var hasAccess = await _context.TeacherDisciplins
                .AnyAsync(td => td.TeacherId == teacherId && td.DisciplinId == journal.DisciplinId);
            if (!hasAccess)
                return Forbid();

            // Обрабатываем каждую пару student-session
            foreach (var kvp in marks)
            {
                // ключ формата "cell_<studentId>_<sessionNumber>"
                var parts = kvp.Key.Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3 || parts[0] != "cell")
                    continue;

                if (!int.TryParse(parts[1], out var studentId))
                    continue;
                if (!int.TryParse(parts[2], out var sessionNumber))
                    continue;

                var markValue = kvp.Value?.Trim();
                if (string.IsNullOrEmpty(markValue))
                {
                    // Если ничего не выбрано (пустая строка), то удаляем запись, если она есть
                    var existingDel = await _context.CourseJournalEntries
                        .FirstOrDefaultAsync(e =>
                            e.JournalId == journalId &&
                            e.StudentId == studentId &&
                            e.SessionNumber == sessionNumber);
                    if (existingDel != null)
                    {
                        _context.CourseJournalEntries.Remove(existingDel);
                    }
                    continue;
                }

                // Ищем существующую запись
                var existing = await _context.CourseJournalEntries
                    .FirstOrDefaultAsync(e =>
                        e.JournalId == journalId &&
                        e.StudentId == studentId &&
                        e.SessionNumber == sessionNumber);

                if (existing == null)
                {
                    // Создаём новую запись
                    var entry = new CourseJournalEntry
                    {
                        JournalId = journalId,
                        StudentId = studentId,
                        SessionNumber = sessionNumber,
                        Mark = markValue
                    };
                    _context.CourseJournalEntries.Add(entry);
                }
                else
                {
                    // Обновляем уже существующую
                    existing.Mark = markValue;
                    _context.CourseJournalEntries.Update(existing);
                }
            }

            await _context.SaveChangesAsync();
            // После сохранения снова показываем страницу журнала
            return RedirectToAction(nameof(Index), new { id = journal.DisciplinId });
        }

        [HttpGet]
        public async Task<IActionResult> Export(int journalId)
        {
            // 1) Получаем сам журнал (CourseJournal) вместе с дисциплиной
            var journal = await _context.CourseJournals
                .Include(j => j.Disciplin)
                .FirstOrDefaultAsync(j => j.Id == journalId);

            if (journal == null)
                return NotFound("Журнал не найден");

            int sessionCount = journal.SessionCount;
            string disciplineName = journal.Disciplin.Name;

            // 2) Найдём группы, которые участвуют в курсах этой дисциплины
            //    (предполагаем, что у вас есть таблица Course с полями DisciplinId и StudentGroupId)
            var groupIds = await _context.Courses
                .Where(c => c.DisciplinId == journal.DisciplinId)
                .Select(c => c.StudentGroupId)
                .Distinct()
                .ToListAsync();

            // 3) Получим всех студентов из этих групп
            var allStudents = await _context.Students
                .Where(s => groupIds.Contains(s.GroupsID))
                .OrderBy(s => s.Surname)
                .ThenBy(s => s.Name)
                .ThenBy(s => s.Patronymic)
                .ToListAsync();

            // 4) Получаем все записи (CourseJournalEntry) этого журнала
            //    Вместе с информацией о студенте (чтобы не делать отдельные запросы)
            var entries = await _context.CourseJournalEntries
                .Where(e => e.JournalId == journalId)
                .ToListAsync();

            // 5) Подготавливаем словарь: (StudentId → Dictionary<SessionNumber, Mark>)
            //    Для удобства собираем все записи в структуру:
            var marksByStudent = entries
                .GroupBy(e => e.StudentId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        // Для каждого студента собираем словарь:
                        // { [1] = отметка для занятия 1, [2] = отметка для занятия 2, … }
                        var dict = new Dictionary<int, string>();
                        for (int i = 1; i <= sessionCount; i++)
                        {
                            // Ищем запись для этого занятия, если она есть
                            var found = g.FirstOrDefault(r => r.SessionNumber == i);
                            dict[i] = found != null ? (found.Mark ?? "") : "";
                        }
                        return dict;
                    }
                );

            // 6) Генерируем Excel-файл через EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Журнал");

            // 6.1) Шапка: «Студент» и «Занятие 1»…«Занятие N»
            worksheet.Cells[1, 1].Value = "Студент";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Column(1).Width = 40;

            for (int col = 2; col <= sessionCount + 1; col++)
            {
                worksheet.Cells[1, col].Value = $"Занятие {col - 1}";
                worksheet.Cells[1, col].Style.Font.Bold = true;
                worksheet.Column(col).Width = 12;
                worksheet.Cells[1, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // 6.2) Заполняем список студентов (в алфавитном порядке)
            int row = 2;
            foreach (var stud in allStudents)
            {
                // Полное имя
                string fullName = stud.Surname + " " + stud.Name +
                                  (string.IsNullOrWhiteSpace(stud.Patronymic)
                                        ? ""
                                        : " " + stud.Patronymic);

                worksheet.Cells[row, 1].Value = fullName;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                // Получаем словарь отметок (если есть), иначе пустой словарь
                if (marksByStudent.TryGetValue(stud.Id, out var studentMarks))
                {
                    // Если записи существуют, заполняем ячейки по словарю
                    for (int s = 1; s <= sessionCount; s++)
                    {
                        worksheet.Cells[row, s + 1].Value = studentMarks[s];
                        worksheet.Cells[row, s + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                }
                else
                {
                    // Если у студента нет ни одной записи, просто делаем все ячейки пустыми
                    for (int s = 1; s <= sessionCount; s++)
                    {
                        worksheet.Cells[row, s + 1].Value = "";
                        worksheet.Cells[row, s + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                }

                row++;
            }

            // 6.3) Обводка вокруг области данных
            var tblRange = worksheet.Cells[1, 1, row - 1, sessionCount + 1];
            tblRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            tblRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            tblRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            tblRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // 7) Записываем в поток и возвращаем файл пользователю
            var stream = new System.IO.MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"Журнал_{disciplineName}_{DateTime.UtcNow:ddMMMyyyy}.xlsx";
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
