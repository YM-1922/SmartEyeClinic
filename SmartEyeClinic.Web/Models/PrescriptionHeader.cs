using System;
using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class PrescriptionHeader
    {
        public int Id { get; set; }
        public int ExaminationId { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Navigation properties
        public virtual Examination Examination { get; set; } = null!;
        public virtual ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new HashSet<PrescriptionItem>();
    }
}
