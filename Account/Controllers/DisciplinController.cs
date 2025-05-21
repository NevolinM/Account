using Account.Data;
using Account.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace Account.Controllers
{
    public class DisciplinController : Controller
    {
        private readonly ILogger<DisciplinController> _logger;
        private readonly appdbcontext _context;

        public DisciplinController(ILogger<DisciplinController> logger, appdbcontext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Create_Disciplin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create_Disciplin(string name, string description)
        {
            _context.Disciplin.Add(new Models.Disciplin{ Name = name, Description = description });
            await _context.SaveChangesAsync();
            return Redirect("/Home/Disciplin");
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
