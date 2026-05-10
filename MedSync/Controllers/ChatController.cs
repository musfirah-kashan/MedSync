using MedSync.Data;
using MedSync.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MedSync.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ── Patient: doctor list + open chat ──
        [HttpGet]
        public async Task<IActionResult> PatientChat(string doctorUserId = null)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated ||
                !User.IsInRole("Patient"))
                return RedirectToAction("Login", "Patient");

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return RedirectToAction("Login", "Patient");

            // Sab doctors
            var doctors = _context.Doctors
                .Select(d => new {
                    d.Id,
                    d.FullName,
                    d.Specialization,
                    d.Department,
                    d.UserId,
                    d.ProfileImagePath
                }).ToList();

            // Unread counts per doctor
            var unreadCounts = _context.ChatMessages
                .Where(m => m.ReceiverId == me.Id && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new { SenderId = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.Doctors = doctors;
            ViewBag.UnreadCounts = unreadCounts;
            ViewBag.Me = me;
            ViewBag.PatientName = me.FullName;

            if (!string.IsNullOrEmpty(doctorUserId))
            {
                var doctorUser = await _userManager.FindByIdAsync(doctorUserId);
                if (doctorUser == null) return NotFound();

                // Messages mark as read
                var unread = _context.ChatMessages
                    .Where(m => m.SenderId == doctorUserId &&
                                m.ReceiverId == me.Id && !m.IsRead)
                    .ToList();
                unread.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();

                var messages = _context.ChatMessages
                    .Where(m => (m.SenderId == me.Id && m.ReceiverId == doctorUserId) ||
                                (m.SenderId == doctorUserId && m.ReceiverId == me.Id))
                    .OrderBy(m => m.SentAt)
                    .ToList();

                ViewBag.ActiveDoctorUserId = doctorUserId;
                ViewBag.ActiveDoctorName = doctorUser.FullName;
                ViewBag.ActiveDoctorSpec = doctorUser.Specialization;
                ViewBag.Messages = messages;
            }

            return View("~/Views/Patient/PatientChat.cshtml");
        }

        // ── Patient: send message ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientSend(string receiverId, string message)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated ||
                !User.IsInRole("Patient"))
                return RedirectToAction("Login", "Patient");

            if (string.IsNullOrWhiteSpace(message))
                return RedirectToAction("PatientChat", new { doctorUserId = receiverId });

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return RedirectToAction("Login", "Patient");

            _context.ChatMessages.Add(new ChatMessage
            {
                SenderId = me.Id,
                ReceiverId = receiverId,
                Message = message.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            });
            await _context.SaveChangesAsync();

            return RedirectToAction("PatientChat", new { doctorUserId = receiverId });
        }

        // ── Doctor: patient list + open chat ──
        [HttpGet]
        public async Task<IActionResult> DoctorChat(string patientUserId = null)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated ||
                !User.IsInRole("Doctor"))
                return RedirectToAction("Login", "Doctor");

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return RedirectToAction("Login", "Doctor");

            var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == me.Id);
            if (doctor == null) return RedirectToAction("Login", "Doctor");

            // Load doctor stats for sidebar badge
            var all = _context.Appointments.Where(a => a.DoctorId == doctor.Id).ToList();
            ViewBag.TotalCount = all.Count;
            ViewBag.PendingCount = all.Count(a => a.Status == "Pending");
            ViewBag.ConfirmedCount = all.Count(a => a.Status == "Confirmed");
            ViewBag.RejectedCount = all.Count(a => a.Status == "Rejected");
            ViewBag.Name = me.FullName;

            // Patients who messaged this doctor
            var patientIds = _context.ChatMessages
                .Where(m => m.ReceiverId == me.Id || m.SenderId == me.Id)
                .Select(m => m.SenderId == me.Id ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            // Also patients who had appointments
            var apptPatientIds = _context.Appointments
                .Where(a => a.DoctorId == doctor.Id && a.UserId != null)
                .Select(a => a.UserId)
                .Distinct()
                .ToList();

            var allPatientIds = patientIds.Union(apptPatientIds).Distinct().ToList();
            var patientUsers = new System.Collections.Generic.List<ApplicationUser>();

            foreach (var pid in allPatientIds)
            {
                var pu = await _userManager.FindByIdAsync(pid);
                if (pu != null) patientUsers.Add(pu);
            }

            var unreadCounts = _context.ChatMessages
                .Where(m => m.ReceiverId == me.Id && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new { SenderId = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.Patients = patientUsers;
            ViewBag.UnreadCounts = unreadCounts;
            ViewBag.Me = me;

            if (!string.IsNullOrEmpty(patientUserId))
            {
                var patientUser = await _userManager.FindByIdAsync(patientUserId);
                if (patientUser == null) return NotFound();

                var unread = _context.ChatMessages
                    .Where(m => m.SenderId == patientUserId &&
                                m.ReceiverId == me.Id && !m.IsRead)
                    .ToList();
                unread.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();

                var messages = _context.ChatMessages
                    .Where(m => (m.SenderId == me.Id && m.ReceiverId == patientUserId) ||
                                (m.SenderId == patientUserId && m.ReceiverId == me.Id))
                    .OrderBy(m => m.SentAt)
                    .ToList();

                ViewBag.ActivePatientUserId = patientUserId;
                ViewBag.ActivePatientName = patientUser.FullName;
                ViewBag.Messages = messages;
            }

            return View("~/Views/Doctor/DoctorChat.cshtml");
        }

        // ── Doctor: send message ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorSend(string receiverId, string message)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated ||
                !User.IsInRole("Doctor"))
                return RedirectToAction("Login", "Doctor");

            if (string.IsNullOrWhiteSpace(message))
                return RedirectToAction("DoctorChat", new { patientUserId = receiverId });

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return RedirectToAction("Login", "Doctor");

            _context.ChatMessages.Add(new ChatMessage
            {
                SenderId = me.Id,
                ReceiverId = receiverId,
                Message = message.Trim(),
                SentAt = DateTime.Now,
                IsRead = false
            });
            await _context.SaveChangesAsync();

            return RedirectToAction("DoctorChat", new { patientUserId = receiverId });
        }

        // ── AJAX: get new messages (polling) ──
        [HttpGet]
        public async Task<IActionResult> GetMessages(string otherUserId, int lastId = 0)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Unauthorized();

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Unauthorized();

            var messages = _context.ChatMessages
                .Where(m =>
                    m.Id > lastId &&
                    ((m.SenderId == me.Id && m.ReceiverId == otherUserId) ||
                     (m.SenderId == otherUserId && m.ReceiverId == me.Id)))
                .OrderBy(m => m.SentAt)
                .Select(m => new {
                    m.Id,
                    m.Message,
                    m.SentAt,
                    isMine = m.SenderId == me.Id,
                    time = m.SentAt.ToString("hh:mm tt")
                })
                .ToList();

            // Mark as read
            var unread = _context.ChatMessages
                .Where(m => m.SenderId == otherUserId &&
                            m.ReceiverId == me.Id &&
                            m.Id > lastId && !m.IsRead)
                .ToList();
            unread.ForEach(m => m.IsRead = true);
            await _context.SaveChangesAsync();

            return Json(messages);
        }
    }
}