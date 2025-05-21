using System.Diagnostics;
using Account.Data;
using Account.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IActionResult> Index()
        {
            List<Disciplin> disciplines = await _context.Disciplin.ToListAsync();
            return View(disciplines);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDisciplin(string disciplineName)
        {
            var discipline = await _context.Disciplin
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

            return Redirect("/Home/Disciplin");
        }

        public async Task<IActionResult> Disciplin()
        {
            List<Disciplin> disciplines = await _context.Disciplin.ToListAsync();
            return View(disciplines);
        }


        public IActionResult Create_Disciplin()
        {
            return View();
        }

        public IActionResult Courses()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
