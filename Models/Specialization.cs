using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Specialization
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Doctor> Doctors { get; set; } = new HashSet<Doctor>();
    }
}
