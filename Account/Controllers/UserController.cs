using Account.Models;
using Account.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Account.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ITeacherService _svc;

        public UserController(ITeacherService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> Profile(string? returnUrl = null)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var id))
                return Unauthorized();

            var teacher = await _svc.GetByIdAsync(id);
            if (teacher == null) return NotFound();

            var vm = new ProfileViewModel
            {
                Id = teacher.Id,
                Surname = teacher.Surname,
                Name = teacher.Name,
                Patronymic = teacher.Patronymic,
                Login = teacher.Login,
                Email = teacher.Email,
                Phone = teacher.Phone,
                Role = teacher.Role,
                PhotoUrl = teacher.Photo,
                ReturnUrl = returnUrl
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var id))
                return Unauthorized();

            var teacher = await _svc.GetByIdAsync(id);
            if (teacher == null)
                return NotFound();

            var vm = new ProfileViewModel
            {
                Id = teacher.Id,
                Surname = teacher.Surname,
                Name = teacher.Name,
                Patronymic = teacher.Patronymic,
                Login = teacher.Login,
                Email = teacher.Email,
                Phone = teacher.Phone,
                Role = teacher.Role,
                PhotoUrl = teacher.Photo
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var teacher = await _svc.GetByIdAsync(vm.Id);
            if (teacher == null)
                return NotFound();

            teacher.Surname = vm.Surname;
            teacher.Name = vm.Name;
            teacher.Patronymic = vm.Patronymic;
            teacher.Email = vm.Email;
            teacher.Phone = vm.Phone;

            if (vm.Photo != null)
            {
                var url = await _svc.SaveProfilePhotoAsync(vm.Id, vm.Photo);
                if (!string.IsNullOrEmpty(url))
                    teacher.Photo = url;
            }

            await _svc.UpdateAsync(teacher);

            // Здесь мы предполагаем, что форма была открыта через window.open(...)
            var script = @"<script>
                               if (window.opener) {
                                   window.opener.location.reload();
                               }
                               window.close();
                           </script>";
            return Content(script, "text/html");
        }
    }
}
