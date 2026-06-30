using System;
using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? Tax { get; set; }
        public decimal? Discount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? IssuedAt { get; set; }

        // Navigation properties
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual Patient Patient { get; set; } = null!;
        public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
    }
}
