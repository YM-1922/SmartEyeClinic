using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = null!;
        public string? Title { get; set; }
        public string? Channel { get; set; }
        public string Message { get; set; } = null!;
        public bool? IsRead { get; set; }
        public DateTime? SentAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
