using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Queue
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public int QueueNumber { get; set; }
        public int? Priority { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CheckInTime { get; set; }
        public DateTime? CalledAt { get; set; }
        public DateTime? EstimatedTime { get; set; }

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
    }
}
