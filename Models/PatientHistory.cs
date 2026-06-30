using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class PatientHistory
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string DiseaseName { get; set; } = null!;
        public DateOnly? DiagnosedDate { get; set; }
        public string? Notes { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
    }
}
