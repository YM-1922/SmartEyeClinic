using System;
using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class Appointment
    {
        public int Id { get; set; } // المعرف الفريد للموعد
        public int PatientId { get; set; } // معرف المريض المرتبط بالموعد
        public int DoctorId { get; set; } // معرف الطبيب المرتبط بالموعد
        public int? ReceptionistId { get; set; } // معرف موظف الاستقبال الذي سجل الموعد (إن وجد)
        public int BranchId { get; set; } // معرف الفرع الخاص بالعيادة
        public DateTime AppointmentDateTime { get; set; } // تاريخ ووقت الموعد
        public int DurationMinutes { get; set; } // مدة الموعد بالدقائق
        public string Type { get; set; } = null!; // نوع الموعد (كشف، استشارة، إلخ)
        public string Status { get; set; } = null!; // حالة الموعد (قيد الانتظار، مقبول، مكتمل، ملغي)
        public string? Notes { get; set; } // ملاحظات إضافية حول الموعد
        public DateTime? CreatedAt { get; set; } // تاريخ تسجيل الموعد في النظام

        // تفاصيل مبلغ الحجز (الوديعة)
        public decimal DepositAmount { get; set; } // قيمة العربون أو مبلغ التأكيد المدفوع
        public string DepositStatus { get; set; } = "Pending"; // حالة دفع العربون (معلق، مدفوع، مسترجع)
        public DateTime? PaymentDate { get; set; } // تاريخ دفع العربون

        // خصائص التنقل البرمجية (العلاقات المرجعية)
        public virtual Patient Patient { get; set; } = null!; // المريض صاحب الموعد
        public virtual Doctor Doctor { get; set; } = null!; // الطبيب المعالج
        public virtual Receptionist? Receptionist { get; set; } // موظف الاستقبال المسؤول
        public virtual Branch Branch { get; set; } = null!; // الفرع الذي سيتم فيه الكشف

        public virtual Queue? Queue { get; set; } // سجل دور المريض في طابور الانتظار بالعيادة
        public virtual Examination? Examination { get; set; } // الفحص الطبي العيني المرتبط بالموعد
        public virtual Surgery? Surgery { get; set; } // العملية الجراحية المرتبطة بالموعد إن وجد
        public virtual Invoice? Invoice { get; set; } // الفاتورة المالية الصادرة لهذا الموعد
        
        public virtual ICollection<MedicalFile> MedicalFiles { get; set; } = new HashSet<MedicalFile>(); // الملفات الطبية المرفوعة ضمن هذا الموعد
    }
}
