using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Branch
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? Phone { get; set; }

        // Navigation properties
        public virtual ICollection<DoctorBranch> DoctorBranches { get; set; } = new HashSet<DoctorBranch>();
        public virtual ICollection<Receptionist> Receptionists { get; set; } = new HashSet<Receptionist>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new HashSet<Appointment>();
    }
}
