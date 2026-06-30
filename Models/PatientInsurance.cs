using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class PatientInsurance
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int InsuranceCompanyId { get; set; }
        public string? InsuranceNumber { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual InsuranceCompany InsuranceCompany { get; set; } = null!;
    }
}
