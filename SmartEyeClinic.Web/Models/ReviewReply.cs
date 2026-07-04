using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class ReviewReply
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual DoctorReview Review { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
