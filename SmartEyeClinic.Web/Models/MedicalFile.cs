using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class MedicalFile
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? AppointmentId { get; set; }
        public int? UploadedBy { get; set; }
        public string FileType { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public long? FileSize { get; set; }
        public DateTime? UploadedAt { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Appointment? Appointment { get; set; }
        public virtual User? Uploader { get; set; }
    }
}
