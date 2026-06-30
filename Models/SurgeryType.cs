using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class SurgeryType
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Surgery> Surgeries { get; set; } = new HashSet<Surgery>();
    }
}
