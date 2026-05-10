using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedSync.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }

        [Required]
        public string ReceiverId { get; set; }

        [Required]
        public string Message { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        // Navigation
        public ApplicationUser? Sender { get; set; }
        public ApplicationUser? Receiver { get; set; }
    }
}