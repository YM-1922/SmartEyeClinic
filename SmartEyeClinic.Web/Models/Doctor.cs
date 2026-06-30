using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Doctor
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SpecializationId { get; set; }
        public string LicenseNumber { get; set; } = null!;
        public decimal ConsultationFee { get; set; }
        public string? Bio { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Specialization Specialization { get; set; } = null!;
        
        public virtual ICollection<DoctorBranch> DoctorBranches { get; set; } = new HashSet<DoctorBranch>();
        public virtual ICollection<DoctorSchedule> Schedules { get; set; } = new HashSet<DoctorSchedule>();
        public virtual ICollection<DoctorReview> Reviews { get; set; } = new HashSet<DoctorReview>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new HashSet<Appointment>();
        public virtual ICollection<Surgery> Surgeries { get; set; } = new HashSet<Surgery>();
    }
}
