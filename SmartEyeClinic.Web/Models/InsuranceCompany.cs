using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class InsuranceCompany
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }

        // Navigation properties
        public virtual ICollection<PatientInsurance> PatientInsurances { get; set; } = new HashSet<PatientInsurance>();
    }
}
