using MedSync.Data;
using MedSync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSync.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string AdminEmail = "admin@medsync.com";
        private const string AdminPassword = "123456";

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdminLoggedIn() =>
            HttpContext.Session.GetString("Admin") == AdminEmail;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Login(string email, string password)
        {
            if (email == AdminEmail && password == AdminPassword)
            {
                HttpContext.Session.SetString("Admin", email);
                return RedirectToAction("Index");
            }
            ViewBag.Error = "Invalid email or password";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult Index()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View();
        }

        public IActionResult Doctors()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View(_context.Doctors.ToList());
        }

        public IActionResult Patients()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View("patients");
        }

        public IActionResult Appointments()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View("Appointment");
        }

        public IActionResult DoctorSchedule()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return RedirectToAction("AddSchedule");
        }

        public IActionResult Departments()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View();
        }

        public IActionResult EmployeesList()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View("employees");
        }

        public IActionResult Leaves()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View("leaves");
        }

        public IActionResult Holidays()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View("holidays");
        }

        public IActionResult Invoices()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View("invoices");
        }

        public IActionResult Payments()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View("payments");
        }

        // ===== SCHEDULE =====
        [HttpGet]
        public IActionResult AddSchedule()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");

            ViewBag.Doctors = _context.Doctors.ToList();
            ViewBag.Schedules = _context.DoctorSchedules
                                    .Include(s => s.Doctor)
                                    .ToList();
            return View();
        }

        [HttpPost]
        public IActionResult AddSchedule(DoctorSchedule schedule)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                ViewBag.Error = "Validation failed: " + string.Join(" | ", errors);
                ViewBag.Doctors = _context.Doctors.ToList();
                ViewBag.Schedules = _context.DoctorSchedules
                                        .Include(s => s.Doctor)
                                        .ToList();
                return View(schedule);
            }

            var exists = _context.DoctorSchedules
                .Any(s => s.DoctorId == schedule.DoctorId &&
                          s.Day == schedule.Day);

            if (exists)
            {
                ViewBag.Error = schedule.Day + " schedule already exists for this doctor.";
            }
            else
            {
                _context.DoctorSchedules.Add(schedule);
                _context.SaveChanges();
                ViewBag.Success = "Schedule added successfully!";
            }

            ViewBag.Doctors = _context.Doctors.ToList();
            ViewBag.Schedules = _context.DoctorSchedules
                                    .Include(s => s.Doctor)
                                    .ToList();
            return View(schedule);
        }

        public IActionResult DeleteSchedule(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");

            var s = _context.DoctorSchedules.Find(id);
            if (s != null)
            {
                _context.DoctorSchedules.Remove(s);
                _context.SaveChanges();
            }
            return RedirectToAction("AddSchedule");
        }
    }
}