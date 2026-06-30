using System;
using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? NationalId { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContact { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<PatientHistory> Histories { get; set; } = new HashSet<PatientHistory>();
        public virtual ICollection<DoctorReview> Reviews { get; set; } = new HashSet<DoctorReview>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new HashSet<Appointment>();
        public virtual ICollection<MedicalFile> MedicalFiles { get; set; } = new HashSet<MedicalFile>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new HashSet<Invoice>();
        public virtual ICollection<PatientInsurance> Insurances { get; set; } = new HashSet<PatientInsurance>();
    }
}
