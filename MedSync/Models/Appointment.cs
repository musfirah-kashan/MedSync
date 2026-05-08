using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedSync.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public string PatientName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        public string Reason { get; set; }

        public string Status { get; set; } = "Pending";

        public int DoctorScheduleId { get; set; }

        public string? UserId { get; set; } 

        public Doctor? Doctor { get; set; }
        public DoctorSchedule? DoctorSchedule { get; set; }
    }
}