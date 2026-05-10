using MedSync.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedSync.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor).WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.DoctorSchedule).WithMany()
                .HasForeignKey(a => a.DoctorScheduleId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DoctorSchedule>()
                .HasOne(ds => ds.Doctor).WithMany()
                .HasForeignKey(ds => ds.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Appointment).WithMany()
                .HasForeignKey(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Doctor).WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            // Chat — no cascade
            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Sender).WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Receiver).WithMany()
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}