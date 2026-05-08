using MedSync.Data;
using MedSync.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MedSync.Controllers
{
    public class DoctorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public DoctorController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        private void LoadDoctorStats(int doctorId)
        {
            var all = _context.Appointments
                .Where(a => a.DoctorId == doctorId).ToList();

            ViewBag.TotalCount = all.Count;
            ViewBag.PendingCount = all.Count(a => a.Status == "Pending");
            ViewBag.ConfirmedCount = all.Count(a => a.Status == "Confirmed");
            ViewBag.RejectedCount = all.Count(a => a.Status == "Rejected");
        }

        // ══════════════════════════
        // REGISTER GET
        // ══════════════════════════
        public IActionResult Register() => View();

        // ══════════════════════════
        // LOGIN GET
        // ══════════════════════════
        public IActionResult Login()
        {
            if (User.Identity != null &&
                User.Identity.IsAuthenticated &&
                User.IsInRole("Doctor"))
                return RedirectToAction("Dashboard");

            return View();
        }

        // ══════════════════════════
        // DASHBOARD
        // ══════════════════════════
        public async Task<IActionResult> Dashboard()
        {
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated ||
                !User.IsInRole("Doctor"))
                return RedirectToAction("Login");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var doctor = _context.Doctors
                .FirstOrDefault(d => d.UserId == user.Id);
            if (doctor == null) return RedirectToAction("Login");

            LoadDoctorStats(doctor.Id);

            ViewBag.Name = user.FullName;
            ViewBag.Email = user.Email;
            ViewBag.Specialization = user.Specialization;
            ViewBag.Department = user.Department;
            ViewBag.Image = user.ProfilePicture;

            ViewBag.RecentAppointments = _context.Appointments
                .Where(a => a.DoctorId == doctor.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .Take(5)
                .ToList();

            return View();
        }

        // ══════════════════════════
        // APPOINTMENTS LIST — sirf ek
        // ══════════════════════════
        public async Task<IActionResult> Appointments(string filter = "All")
        {
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated ||
                !User.IsInRole("Doctor"))
                return RedirectToAction("Login");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var doctor = _context.Doctors
                .FirstOrDefault(d => d.UserId == user.Id);
            if (doctor == null) return RedirectToAction("Login");

            LoadDoctorStats(doctor.Id);

            ViewBag.Name = user.FullName;
            ViewBag.Filter = filter;

            var query = _context.Appointments
                .Where(a => a.DoctorId == doctor.Id);

            if (filter != "All")
                query = query.Where(a => a.Status == filter);

            ViewBag.Appointments = query
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            return View();
        }

        // ══════════════════════════
        // UPDATE STATUS
        // ══════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status,
                                                       string returnUrl = "Dashboard")
        {
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated ||
                !User.IsInRole("Doctor"))
                return RedirectToAction("Login");

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction(returnUrl == "Appointments"
                    ? "Appointments" : "Dashboard");
            }

            var user = await _userManager.GetUserAsync(User);
            var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == user.Id);

            if (doctor == null || appointment.DoctorId != doctor.Id)
            {
                TempData["Error"] = "Unauthorized.";
                return RedirectToAction("Dashboard");
            }

            appointment.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = status switch
            {
                "Confirmed" => "Appointment approved successfully.",
                "Rejected" => "Appointment rejected.",
                "Completed" => "Appointment marked as completed.",
                _ => "Status updated."
            };

            return RedirectToAction(returnUrl == "Appointments"
                ? "Appointments" : "Dashboard");
        }

        // ══════════════════════════
        // REGISTER POST
        // ══════════════════════════
        [HttpPost]
        public async Task<IActionResult> Register(Doctor model)
        {
            string uniqueFileName = null;

            if (model.ProfileImage != null)
            {
                string uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot/images");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                uniqueFileName = Guid.NewGuid().ToString() + "_" +
                                 model.ProfileImage.FileName;

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await model.ProfileImage.CopyToAsync(stream);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Specialization = model.Specialization,
                Department = model.Department,
                ProfilePicture = uniqueFileName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return Content("Error: " +
                    string.Join(" | ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, "Doctor");

            var doctor = new Doctor
            {
                FullName = model.FullName,
                Email = model.Email,
                Specialization = model.Specialization,
                Department = model.Department,
                UserId = user.Id,
                ProfileImagePath = uniqueFileName
            };

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Login", "Doctor");
        }

        // ══════════════════════════
        // LOGIN POST
        // ══════════════════════════
        [HttpPost]
        public async Task<IActionResult> Login(LoginDoctor model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, false);

            if (result.Succeeded)
                return RedirectToAction("Dashboard");

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        // ══════════════════════════
        // LOGOUT
        // ══════════════════════════
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Doctor");
        }
    }
}