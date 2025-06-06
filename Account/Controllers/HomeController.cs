using Account.Data;
using Account.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Account.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly appdbcontext _context;

        public HomeController(ILogger<HomeController> logger, appdbcontext context)
        {
            _logger = logger;
            _context = context;
        }

        // Вспомогательный метод для получения ID преподавателя из claims
        private int? GetTeacherId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        public async Task<IActionResult> Index()
        {
            var teacherId = GetTeacherId();
            if (teacherId == null)
                return Unauthorized();

            var disciplines = await _context.TeacherDisciplins
                .Where(td => td.TeacherId == teacherId)
                .Select(td => td.Disciplin)
                .ToListAsync();
            return View(disciplines);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDisciplin(string disciplineName)
        {
            var teacherId = GetTeacherId();
            if (teacherId == null)
                return Unauthorized();

            var discipline = await _context.TeacherDisciplins
                .Where(td => td.TeacherId == teacherId)
                .Select(td => td.Disciplin)
                .FirstOrDefaultAsync(d => d.Name == disciplineName);

            if (discipline != null)
            {
                _context.Disciplin.Remove(discipline);
                await _context.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Дисциплина не найдена");
            }

            return RedirectToAction(nameof(Disciplin));
        }

        public async Task<IActionResult> Disciplin()
        {
            var teacherId = GetTeacherId();
            if (teacherId == null)
                return Unauthorized();

            var disciplines = await _context.TeacherDisciplins
                .Where(td => td.TeacherId == teacherId)
                .Select(td => td.Disciplin)
                .ToListAsync();
            return View(disciplines);
        }


        public IActionResult Create_Disciplin()
        {
            return View();
        }

        public async Task<IActionResult> Courses()
        {
            var teacherId = GetTeacherId();
            if (teacherId == null)
                return Unauthorized();

            var list = await _context.Disciplin
                .Where(d => d.TeacherDisciplins.Any(td => td.TeacherId == teacherId))
                .Include(d => d.Courses)
                    .ThenInclude(c => c.StudentGroup)
                .ToListAsync();

            ViewBag.AllGroups = await _context.StudentGroups.ToListAsync();
            return View(list);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
