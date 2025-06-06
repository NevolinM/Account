using Account.Data;
using Account.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Account.Controllers
{
    public class CourseController : Controller
    {
        private readonly appdbcontext _context;
        public CourseController(appdbcontext context) => _context = context;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course model)
        {
            if (!ModelState.IsValid)
            {
                _context.Courses.Add(model);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Courses", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int disciplinId, int studentGroupId)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c =>
                    c.DisciplinId == disciplinId &&
                    c.StudentGroupId == studentGroupId);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
            // возвращаемся на список
            return RedirectToAction("Courses", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var course = await _context.Courses
                .Include(c => c.StudentGroup)
                .Include(c => c.Disciplin)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var students = await _context.Students
                .Where(s => s.GroupsID == course.StudentGroupId)
                .ToListAsync();
            var vm = new CourseDetailsViewModel
            {
                Course = course,
                Students = students
            };
            return View(vm);
        }
    }
}
