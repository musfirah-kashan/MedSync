using MedSync.Data;
using MedSync.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.Json;

namespace MedSync.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        private void LoadAppointmentViewBag()
        {
            ViewBag.Doctors = _context.Doctors.ToList();

            var schedules = _context.DoctorSchedules
                .Select(ds => new
                {
                    doctorId = ds.DoctorId,
                    day = ds.Day.Trim(),
                    startTime = ds.StartTime.Hours.ToString("D2") + ":" + ds.StartTime.Minutes.ToString("D2"),
                    endTime = ds.EndTime.Hours.ToString("D2") + ":" + ds.EndTime.Minutes.ToString("D2")
                })
                .ToList();

            ViewBag.DoctorSchedulesJson = JsonSerializer.Serialize(schedules,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
        }

        [HttpGet]
        public IActionResult Appointment()
        {
            LoadAppointmentViewBag();
            return View("Appointment");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Book(Appointment appointment)
        {
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

            // TimeSpan directly compare karo
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

            appointment.Status = "Pending";
            appointment.DoctorScheduleId = doctorSchedule.Id;

            _context.Appointments.Add(appointment);
            _context.SaveChanges();

            TempData["Success"] = "Appointment booked successfully!";
            return RedirectToAction("Appointment");
        }
    }
}