using MedSync.Data;
using MedSync.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MedSync.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public PatientController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        private void LoadAppointmentViewBag()
        {
            ViewBag.Doctors = _context.Doctors.ToList();

            var schedules = _context.DoctorSchedules
                .Select(ds => new {
                    doctorId = ds.DoctorId,
                    day = ds.Day.Trim(),
                    startTime = ds.StartTime.Hours.ToString("D2") + ":" +
                                ds.StartTime.Minutes.ToString("D2"),
                    endTime = ds.EndTime.Hours.ToString("D2") + ":" +
                                ds.EndTime.Minutes.ToString("D2")
                }).ToList();

            ViewBag.DoctorSchedulesJson = JsonSerializer.Serialize(schedules,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
        }

        // ══════════════════════════════
        // LOGIN GET
        // ══════════════════════════════
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null &&
                User.Identity.IsAuthenticated &&
                User.IsInRole("Patient"))
                return RedirectToAction("Dashboard");

            return View("Login");
        }

        // ══════════════════════════════
        // LOGIN POST
        // ══════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password,
                                               string returnUrl = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View("Login");
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !await _userManager.IsInRoleAsync(user, "Patient"))
            {
                ViewBag.Error = "No patient account found with this email.";
                return View("Login");
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, password, isPersistent: true, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ViewBag.Error = "Incorrect password. Please try again.";
                return View("Login");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Dashboard");
        }

        // ══════════════════════════════
        // LOGOUT
        // ══════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // ══════════════════════════════
        // APPOINTMENT GET
        // ══════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Appointment()
        {
            LoadAppointmentViewBag();

            if (User.Identity != null &&
                User.Identity.IsAuthenticated &&
                User.IsInRole("Patient"))
            {
                var user = await _userManager.GetUserAsync(User);
                ViewBag.PrefilledEmail = user?.Email;
                ViewBag.PrefilledName = user?.FullName;
            }

            return View("Appointment");
        }

        // ══════════════════════════════
        // BOOK POST
        // ══════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(Appointment appointment)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("DoctorScheduleId");
            ModelState.Remove("Doctor");
            ModelState.Remove("DoctorSchedule");
            ModelState.Remove("Status");

            if (!ModelState.IsValid)
            {
                LoadAppointmentViewBag();
                ViewBag.Error = string.Join(" | ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage));
                return View("Appointment", appointment);
            }

            if (appointment.AppointmentDate == default)
            {
                LoadAppointmentViewBag();
                ViewBag.Error = "Please select a valid date and time slot.";
                return View("Appointment", appointment);
            }

            var appointmentDay = appointment.AppointmentDate.DayOfWeek.ToString();
            var appointmentTime = appointment.AppointmentDate.TimeOfDay;

            var doctorSchedule = _context.DoctorSchedules
                .AsEnumerable()
                .FirstOrDefault(ds =>
                    ds.DoctorId == appointment.DoctorId &&
                    ds.Day.Trim().ToLower() == appointmentDay.ToLower() &&
                    ds.StartTime <= appointmentTime &&
                    ds.EndTime > appointmentTime);

            if (doctorSchedule == null)
            {
                LoadAppointmentViewBag();
                ViewBag.Error = "Selected time is not available in the doctor's schedule.";
                return View("Appointment", appointment);
            }

            var alreadyBooked = _context.Appointments.Any(a =>
                a.DoctorId == appointment.DoctorId &&
                a.AppointmentDate == appointment.AppointmentDate &&
                a.Status != "Cancelled");

            if (alreadyBooked)
            {
                LoadAppointmentViewBag();
                ViewBag.Error = "This slot is already booked. Please choose another.";
                return View("Appointment", appointment);
            }

            ApplicationUser user;
            string generatedPassword = null;
            bool isNewUser = false;

            if (User.Identity != null &&
                User.Identity.IsAuthenticated &&
                User.IsInRole("Patient"))
            {
                user = await _userManager.GetUserAsync(User);
            }
            else
            {
                user = await _userManager.FindByEmailAsync(appointment.Email);

                if (user == null)
                {
                    var namePart = appointment.PatientName.Length >= 4
                        ? appointment.PatientName.Substring(0, 4)
                        : appointment.PatientName;

                    var phonePart = appointment.Phone.Length >= 4
                        ? appointment.Phone.Substring(appointment.Phone.Length - 4)
                        : appointment.Phone;

                    generatedPassword = namePart + "@" + phonePart;
                    isNewUser = true;

                    user = new ApplicationUser
                    {
                        UserName = appointment.Email,
                        Email = appointment.Email,
                        FullName = appointment.PatientName
                    };

                    var createResult = await _userManager.CreateAsync(
                        user, generatedPassword);

                    if (!createResult.Succeeded)
                    {
                        LoadAppointmentViewBag();
                        ViewBag.Error = "Account creation failed: " +
                            string.Join(", ",
                                createResult.Errors.Select(e => e.Description));
                        return View("Appointment", appointment);
                    }

                    await _userManager.AddToRoleAsync(user, "Patient");
                }

                await _signInManager.SignInAsync(user, isPersistent: true);
            }

            appointment.Status = "Pending";
            appointment.DoctorScheduleId = doctorSchedule.Id;
            appointment.UserId = user.Id;

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment booked successfully!";

            if (isNewUser)
            {
                TempData["ShowPassword"] = "true";
                TempData["GenPassword"] = generatedPassword;
                TempData["PatientEmail"] = appointment.Email;
            }

            return RedirectToAction("Dashboard");
        }

        // ══════════════════════════════
        // DASHBOARD
        // ══════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (User.Identity == null ||
                !User.Identity.IsAuthenticated ||
                !User.IsInRole("Patient"))
                return RedirectToAction("Login");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var appointments = _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();

            var upcoming = appointments
                .Where(a =>
                    a.Status == "Confirmed" &&
                    a.AppointmentDate > DateTime.Now &&
                    a.AppointmentDate <= DateTime.Now.AddDays(2))
                .ToList();

            var rejectedRecent = appointments
                .Where(a =>
                    a.Status == "Rejected" &&
                    a.AppointmentDate >= DateTime.Now.AddDays(-3))
                .ToList();

            ViewBag.PatientName = user.FullName;
            ViewBag.Appointments = appointments;
            ViewBag.Upcoming = upcoming;
            ViewBag.RejectedRecent = rejectedRecent;
            ViewBag.TotalCount = appointments.Count;
            ViewBag.PendingCount = appointments.Count(a => a.Status == "Pending");
            ViewBag.ConfirmedCount = appointments.Count(a => a.Status == "Confirmed");

            return View("Dashboard");
        }
    }
}