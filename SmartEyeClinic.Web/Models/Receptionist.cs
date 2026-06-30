using System;
using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Receptionist
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BranchId { get; set; }
        public TimeOnly? ShiftStart { get; set; }
        public TimeOnly? ShiftEnd { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Branch Branch { get; set; } = null!;
        public virtual ICollection<Appointment> Appointments { get; set; } = new HashSet<Appointment>();
    }
}
