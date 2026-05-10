using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedSync.Models
{
    public class Prescription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public string PatientUserId { get; set; }

        public string Diagnosis { get; set; }

        public string Medicines { get; set; }

        public string Tests { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("AppointmentId")]
        public Appointment? Appointment { get; set; }

        [ForeignKey("DoctorId")]
        public Doctor? Doctor { get; set; }
    }
}