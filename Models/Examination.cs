using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Examination
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public string Diagnosis { get; set; } = null!;
        public string? Symptoms { get; set; }
        public string? VisualAcuityLeft { get; set; }
        public string? VisualAcuityRight { get; set; }
        public string? IntraocularPressure { get; set; }
        public string? TreatmentPlan { get; set; }
        public DateTime? ExaminedAt { get; set; }

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual PrescriptionHeader? PrescriptionHeader { get; set; }
    }
}
