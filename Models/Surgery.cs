using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Surgery
    {
        public int Id { get; set; }

        public int AppointmentId { get; set; }

        public int SurgeryTypeId { get; set; }

        public DateTime SurgeryDate { get; set; }

        public string? Outcome { get; set; }

        public string? Notes { get; set; }

        // Navigation Properties
        public virtual Appointment Appointment { get; set; } = null!;

        public virtual SurgeryType SurgeryType { get; set; } = null!;
    }
}