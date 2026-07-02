using System;
using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? ReceptionistId { get; set; }
        public int BranchId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public int DurationMinutes { get; set; }
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Deposit fields
        public decimal DepositAmount { get; set; }
        public string DepositStatus { get; set; } = "Pending";
        public DateTime? PaymentDate { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual Receptionist? Receptionist { get; set; }
        public virtual Branch Branch { get; set; } = null!;

        public virtual Queue? Queue { get; set; }
        public virtual Examination? Examination { get; set; }
        public virtual Surgery? Surgery { get; set; }
        public virtual Invoice? Invoice { get; set; }
        
        public virtual ICollection<MedicalFile> MedicalFiles { get; set; } = new HashSet<MedicalFile>();
    }
}
