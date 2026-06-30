using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Medicine
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Manufacturer { get; set; }

        // Navigation properties
        public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new HashSet<PrescriptionItem>();
    }
}
