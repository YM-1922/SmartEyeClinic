#nullable enable

namespace SmartEyeClinic.Models
{
    public class PrescriptionItem
    {
        public int Id { get; set; }
        public int PrescriptionId { get; set; }
        public int MedicineId { get; set; }
        public string? Dosage { get; set; }
        public int? DurationDays { get; set; }
        public string? Instructions { get; set; }

        // Navigation properties
        public virtual PrescriptionHeader PrescriptionHeader { get; set; } = null!;
        public virtual Medicine Medicine { get; set; } = null!;
    }
}
