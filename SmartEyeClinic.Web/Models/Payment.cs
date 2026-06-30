using System;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionRef { get; set; }
        public DateTime? PaidAt { get; set; }

        // Navigation properties
        public virtual Invoice Invoice { get; set; } = null!;
        public virtual PaymentMethod PaymentMethod { get; set; } = null!;
    }
}
