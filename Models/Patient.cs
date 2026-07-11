using System;
using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Patient
    {
        public int Id { get; set; } // المعرف الفريد للمريض
        public int UserId { get; set; } // معرف حساب المستخدم المرتبط
        public string? NationalId { get; set; } // الرقم القومي / الهوية الشخصية
        public DateOnly? DateOfBirth { get; set; } // تاريخ الميلاد
        public string? Gender { get; set; } // الجنس
        public string? BloodType { get; set; } // فصيلة الدم
        public string? Address { get; set; } // العنوان السكني
        public string? EmergencyContact { get; set; } // رقم الاتصال في حالات الطوارئ

        // خصائص التنقل البرمجية (العلاقات المرجعية)
        public virtual User User { get; set; } = null!; // حساب المستخدم الأساسي
        public virtual ICollection<PatientHistory> Histories { get; set; } = new HashSet<PatientHistory>(); // التاريخ المرضي وسجلات الحالات السابقة
        public virtual ICollection<DoctorReview> Reviews { get; set; } = new HashSet<DoctorReview>(); // التقييمات والمراجعات التي كتبها المريض للأطباء
        public virtual ICollection<Appointment> Appointments { get; set; } = new HashSet<Appointment>(); // مواعيد الكشوفات الطبية للمريض
        public virtual ICollection<MedicalFile> MedicalFiles { get; set; } = new HashSet<MedicalFile>(); // الملفات الطبية المرفوعة الخاصة بالمريض
        public virtual ICollection<Invoice> Invoices { get; set; } = new HashSet<Invoice>(); // الفواتير المالية الصادرة للمريض
        public virtual ICollection<PatientInsurance> Insurances { get; set; } = new HashSet<PatientInsurance>(); // بيانات التأمين الصحي للمريض
    }
}
