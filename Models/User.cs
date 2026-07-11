using System;
using System.Collections.Generic;

#nullable enable

namespace SmartEyeClinic.Models
{
    public class User
    {
        public int Id { get; set; } // المعرف الفريد للمستخدم
        public string FullName { get; set; } = null!; // الاسم الكامل للمستخدم
        public string Email { get; set; } = null!; // البريد الإلكتروني (يستخدم لتسجيل الدخول)
        public string PasswordHash { get; set; } = null!; // هاش كلمة المرور (مخزن كنص واضح للتبسيط)
        public string? PhoneNumber { get; set; } // رقم الهاتف للتواصل
        public int RoleId { get; set; } // معرف صلاحية المستخدم (الدور)
        public bool IsActive { get; set; } // حالة الحساب نشط أم معطل
        public string? ProfilePicture { get; set; } // مسار صورة الملف الشخصي
        public DateTime CreatedAt { get; set; } // تاريخ إنشاء الحساب
        public DateTime? UpdatedAt { get; set; } // تاريخ آخر تحديث للحساب

        // خصائص التنقل البرمجية (العلاقات المرجعية)
        public virtual Role Role { get; set; } = null!; // الصلاحية/الدور المرتبط
        public virtual Doctor? Doctor { get; set; } // ملف الطبيب (في حال كان دور المستخدم طبيباً)
        public virtual Patient? Patient { get; set; } // ملف المريض (في حال كان دور المستخدم مريضاً)
        public virtual Receptionist? Receptionist { get; set; } // ملف موظف الاستقبال (في حال كان دور المستخدم موظف استقبال)
        
        public virtual ICollection<MedicalFile> MedicalFilesUploaded { get; set; } = new HashSet<MedicalFile>(); // الملفات الطبية المرفوعة بواسطة هذا المستخدم
        public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>(); // قائمة الإشعارات الخاصة بالمستخدم
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new HashSet<AuditLog>(); // سجل العمليات الذي قام به المستخدم
    }
}
