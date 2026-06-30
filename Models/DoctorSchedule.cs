using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class DoctorSchedule
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string DayOfWeek { get; set; } = null!;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool? IsAvailable { get; set; }

        // Navigation properties
        public virtual Doctor Doctor { get; set; } = null!;
    }
}
