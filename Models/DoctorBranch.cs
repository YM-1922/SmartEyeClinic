#nullable enable

namespace SmartEyeClinic.Models
{
    public class DoctorBranch
    {
        public int DoctorId { get; set; }
        public int BranchId { get; set; }

        // Navigation properties
        public virtual Doctor Doctor { get; set; } = null!;
        public virtual Branch Branch { get; set; } = null!;
    }
}
