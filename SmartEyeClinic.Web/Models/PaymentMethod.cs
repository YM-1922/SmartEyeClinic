using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class PaymentMethod
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
    }
}
