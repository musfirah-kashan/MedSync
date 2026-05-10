using MedSync.Data;
using MedSync.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public IActionResult Register() => View();

        public IActionResult Login()
        {
            if (User.Identity != null &&
                User.Identity.IsAuthenticated &&
                User.IsInRole("Doctor"))
                return RedirectToAction("Dashboard");
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated ||
                !User.IsInRole("Doctor"))
                return RedirectToAction("Login");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == user.Id);
            if (doctor == null) return RedirectToAction("Login");

            LoadDoctorStats(doctor.Id);

            ViewBag.Name = user.FullName;
            ViewBag.Email = user.Email;
            ViewBag.Specialization = user.Specialization;
            ViewBag.Department = user.Department;

            ViewBag.RecentAppointments = _context.Appointments
                .Where(a => a.DoctorId == doctor.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .Take(5)
                .ToList();

            return View();
        }

        public async Task<IActionResult> Appointments(string filter = "All")
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated ||
                !User.IsInRole("Doctor"))
                return RedirectToAction("Login");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == user.Id);
            if (doctor == null) return RedirectToAction("Login");

            LoadDoctorStats(doctor.Id);
            ViewBag.Name = user.FullName;
            ViewBag.Filter = filter;

            var query = _context.Appointments.Where(a => a.DoctorId == doctor.Id);
            if (filter != "All")
                query = query.Where(a => a.Status == filter);

            ViewBag.Appointments = query
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            return View();
        }

        // ── Patient History ──
        public async Task<IActionResult> PatientHistory(string userId)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated ||
                !User.IsInRole("Doctor"))
                return RedirectToAction("Login");

            var user = await _userManager.GetUserAsync(User);
            var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == user.Id);
            if (doctor == null) return RedirectToAction("Login");

            var patientUser = await _userManager.FindByIdAsync(userId);
            if (patientUser == null) return NotFound();

            ViewBag.Name = user.FullName;
            ViewBag.PatientName = patientUser.FullName;
            ViewBag.PatientEmail = patientUser.Email;

            LoadDoctorStats(doctor.Id);

            var appointments = _context.Appointments
                .Where(a => a.DoctorId == doctor.Id && a.UserId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            var prescriptions = _context.Prescriptions
                .Include(p => p.Appointment)
                .Where(p => p.DoctorId == doctor.Id && p.PatientUserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            ViewBag.Appointments = appointments;
            ViewBag.Prescriptions = prescriptions;

            return View();
        }

        // ── Add Prescription GET ──
        [HttpGet]
        public async Task<IActionResult> AddPrescription(int? appointmentId)
        {
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated ||
                !User.IsInRole("Doctor"))
            {
                return RedirectToAction("Login");
            }

            if (appointmentId == null)
            {
                TempData["Error"] = "Invalid appointment.";
                return RedirectToAction("Appointments");
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login");

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (doctor == null)
                return RedirectToAction("Login");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.Id == appointmentId &&
                    a.DoctorId == doctor.Id);

            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }

            // Optional:
            // Sirf completed appointment par prescription allow karo

            if (appointment.Status != "Completed")
            {
                TempData["Error"] =
                    "Prescription can only be added after appointment is completed.";

                return RedirectToAction("Appointments");
            }

            var existing = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.AppointmentId == appointment.Id);

            ViewBag.Name = user.FullName;
            ViewBag.Appointment = appointment;
            ViewBag.Existing = existing;

            LoadDoctorStats(doctor.Id);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrescription(Prescription prescription)
        {
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated ||
                !User.IsInRole("Doctor"))
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login");

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (doctor == null)
                return RedirectToAction("Login");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.Id == prescription.AppointmentId &&
                    a.DoctorId == doctor.Id);

            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("Appointments");
            }

            if (appointment.Status != "Completed")
            {
                TempData["Error"] =
                    "Prescription can only be added after appointment completion.";

                return RedirectToAction("Appointments");
            }

            var existing = await _context.Prescriptions
                .FirstOrDefaultAsync(p =>
                    p.AppointmentId == prescription.AppointmentId);

            if (existing != null)
            {
                existing.Diagnosis = prescription.Diagnosis;
                existing.Medicines = prescription.Medicines;
                existing.Tests = prescription.Tests;
                existing.Notes = prescription.Notes;
                existing.CreatedAt = DateTime.Now;

                _context.Prescriptions.Update(existing);

                TempData["Success"] = "Prescription updated successfully.";
            }
            else
            {
                var newPrescription = new Prescription
                {
                    AppointmentId = appointment.Id,
                    DoctorId = doctor.Id,
                    PatientUserId = appointment.UserId,

                    Diagnosis = prescription.Diagnosis,
                    Medicines = prescription.Medicines,
                    Tests = prescription.Tests,
                    Notes = prescription.Notes,

                    CreatedAt = DateTime.Now
                };

                _context.Prescriptions.Add(newPrescription);

                TempData["Success"] = "Prescription added successfully.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "PatientHistory",
                new { userId = appointment.UserId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status,
                                                       string returnUrl = "Dashboard")
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated ||
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

        [HttpPost]
        public async Task<IActionResult> Login(LoginDoctor model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, false);

            if (result.Succeeded) return RedirectToAction("Dashboard");

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Doctor");
        }
    }
}