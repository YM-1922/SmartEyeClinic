using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = null!;
        public string? TableName { get; set; }
        public int? RecordId { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
    }
}
