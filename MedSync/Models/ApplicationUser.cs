using Microsoft.AspNetCore.Identity;
namespace MedSync.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsGuest { get; set; } = false;
        public string? Specialization { get; set; }
        public string? Department { get; set; }
        public string? Role { get; set; }
    }
}