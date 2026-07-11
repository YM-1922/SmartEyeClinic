using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Doctor
    {
        public int Id { get; set; } // المعرف الفريد للطبيب
        public int UserId { get; set; } // معرف حساب المستخدم المرتبط
        public int SpecializationId { get; set; } // معرف التخصص الطبي العيني
        public string LicenseNumber { get; set; } = null!; // رقم رخصة مزاولة المهنة الطبية
        public decimal ConsultationFee { get; set; } // قيمة كشف الطبيب (رسوم الاستشارة)
        public string? Bio { get; set; } // السيرة الذاتية أو نبذة عن الطبيب

        // خصائص التنقل البرمجية (العلاقات المرجعية)
        public virtual User User { get; set; } = null!; // حساب المستخدم الأساسي المرتبط بالطبيب
        public virtual Specialization Specialization { get; set; } = null!; // التخصص الطبي المرتبط
        
        public virtual ICollection<DoctorBranch> DoctorBranches { get; set; } = new HashSet<DoctorBranch>(); // فروع العيادة التي يعمل بها الطبيب
        public virtual ICollection<DoctorSchedule> Schedules { get; set; } = new HashSet<DoctorSchedule>(); // جدول مواعيد وساعات عمل الطبيب
        public virtual ICollection<DoctorReview> Reviews { get; set; } = new HashSet<DoctorReview>(); // تقييمات ومراجعات المرضى للطبيب
        public virtual ICollection<Appointment> Appointments { get; set; } = new HashSet<Appointment>(); // مواعيد الكشوفات الطبية المحجوزة لدى الطبيب
        public virtual ICollection<Surgery> Surgeries { get; set; } = new HashSet<Surgery>(); // العمليات الجراحية التي يجريها الطبيب
    }
}
