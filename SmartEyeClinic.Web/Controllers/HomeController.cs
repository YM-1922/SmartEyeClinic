using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace SmartEyeClinic.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public IActionResult Landing()
    {
        return View();
    }

    [AllowAnonymous]
    public async Task<IActionResult> DoctorAccountsReport()
    {
        var doctors = await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Specialization)
            .ToListAsync();
        return View(doctors);
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(AdminDashboard));
            if (User.IsInRole("Doctor"))
                return RedirectToAction(nameof(DoctorDashboard));
            if (User.IsInRole("Patient"))
                return RedirectToAction(nameof(PatientDashboard));
            if (User.IsInRole("Receptionist"))
                return RedirectToAction(nameof(ReceptionistDashboard));
        }

        return RedirectToAction(nameof(Landing));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminDashboard()
    {
        // إحصائيات النظام العامة لإدارة العيادة (المرضى، الأطباء، الإيرادات، إلخ)
        ViewBag.TotalPatients = await _context.Patients.CountAsync();
        ViewBag.TotalDoctors = await _context.Doctors.CountAsync();
        ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
        ViewBag.TotalRevenue = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;
        ViewBag.TotalSurgeries = await _context.Surgeries.CountAsync();
        ViewBag.TodayAppointmentsCount = await _context.Appointments.CountAsync(a => a.AppointmentDateTime.Date == DateTime.Today);
        ViewBag.UnpaidInvoicesCount = await _context.Invoices.CountAsync(i => i.Status != "Paid");

        // القوائم اللازمة للإدارة والبحث في النظام
        ViewBag.DoctorsList = await _context.Doctors.Include(d => d.User).Include(d => d.Specialization).ToListAsync();
        ViewBag.PatientsList = await _context.Patients.Include(p => p.User).Take(30).ToListAsync();
        ViewBag.AppointmentsList = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .OrderByDescending(a => a.AppointmentDateTime)
            .Take(30).ToListAsync();
        ViewBag.DepositsList = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Where(a => a.DepositStatus == "Paid" || a.DepositStatus == "Pending")
            .OrderByDescending(a => a.CreatedAt)
            .Take(30).ToListAsync();

        // سجل العمليات والتدقيق الأخير (Audit Logs) لضمان أمان النظام وبورصة البيانات
        var auditLogs = await _context.AuditLogs
            .Include(al => al.User)
            .OrderByDescending(al => al.CreatedAt)
            .Take(10)
            .ToListAsync();
        ViewBag.RecentAudits = auditLogs;

        // المدفوعات الأخيرة المستلمة لتتبع الحركة المالية بالعيادة
        var recentPayments = await _context.Payments
            .Include(p => p.Invoice).ThenInclude(i => i.Patient).ThenInclude(pat => pat.User)
            .Include(p => p.PaymentMethod)
            .OrderByDescending(p => p.PaidAt)
            .Take(10)
            .ToListAsync();
        ViewBag.RecentPayments = recentPayments;

        // الإشعارات العامة الأخيرة المرسلة بالنظام لمتابعة الأحداث النشطة
        ViewBag.RecentNotifications = await _context.Notifications
            .OrderByDescending(n => n.SentAt)
            .Take(15)
            .ToListAsync();

        return View();
    }

    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> DoctorDashboard()
    {
        var docIdClaim = User.FindFirst("DoctorId")?.Value;
        if (string.IsNullOrEmpty(docIdClaim))
            return RedirectToAction("AccessDenied", "Account");

        int doctorId = int.Parse(docIdClaim);

        // جلب قائمة بمواعيد الطبيب المقررة لليوم الحالي للبدء بالمعاينة والملاحظات الطبية
        var todayAppointments = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Branch)
            .Where(a => a.DoctorId == doctorId && a.AppointmentDateTime.Date == DateTime.Today)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
        ViewBag.TodayAppointments = todayAppointments;

        // جلب طلبات حجز المواعيد الجديدة المعلقة التي تحتاج لمراجعة وقبول/رفض الطبيب المختص
        var pendingApprovals = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Branch)
            .Where(a => a.DoctorId == doctorId && a.Status == "Pending Approval")
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
        ViewBag.PendingApprovals = pendingApprovals;

        // جلب قائمة المواعيد التي تم سداد مبلغ العربون الخاص بها بنجاح لتأكيد الحجز المبدئي للعيادة
        var confirmedDeposits = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Where(a => a.DoctorId == doctorId && a.DepositStatus == "Paid")
            .OrderByDescending(a => a.PaymentDate)
            .ToListAsync();
        ViewBag.ConfirmedDeposits = confirmedDeposits;

        // جلب سجل زيارات المرضى السابقة المكتملة لدى هذا الطبيب للرجوع للفحوصات الطبية
        var patientHistory = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Where(a => a.DoctorId == doctorId && a.Status == "Completed")
            .OrderByDescending(a => a.AppointmentDateTime)
            .ToListAsync();
        ViewBag.PatientHistory = patientHistory;

        // إحصائيات عامة خاصة بالطبيب الحالي (عدد المرضى، المواعيد المعلقة، العمليات الجراحية)
        ViewBag.TotalMyPatients = await _context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .Select(a => a.PatientId)
            .Distinct()
            .CountAsync();
        ViewBag.MyPendingAppointments = await _context.Appointments
            .CountAsync(a => a.DoctorId == doctorId && a.Status == "Pending Approval");
        ViewBag.MySurgeries = await _context.Surgeries
            .CountAsync(s => s.DoctorId == doctorId);

        // العمليات الجراحية القادمة المجدولة للطبيب الحالي لمراجعة الترتيبات والتحضيرات الطبية بالعيادة
        var upcomingSurgeries = await _context.Surgeries
            .Include(s => s.Patient).ThenInclude(p => p.User)
            .Include(s => s.SurgeryType)
            .Where(s => s.DoctorId == doctorId && s.SurgeryDate >= DateTime.Today)
            .OrderBy(s => s.SurgeryDate)
            .Take(5)
            .ToListAsync();
        ViewBag.UpcomingSurgeries = upcomingSurgeries;

        // جلب آخر 10 إشعارات واردة للطبيب للتعامل الفوري مع أي تحديثات موعد أو إلغاء
        var doctorUserClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(doctorUserClaim))
        {
            int docUserId = int.Parse(doctorUserClaim);
            ViewBag.RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == docUserId)
                .OrderByDescending(n => n.SentAt)
                .Take(10)
                .ToListAsync();
        }

        return View();
    }

    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> PatientDashboard()
    {
        var patIdClaim = User.FindFirst("PatientId")?.Value;
        if (string.IsNullOrEmpty(patIdClaim))
            return RedirectToAction("AccessDenied", "Account");

        int patientId = int.Parse(patIdClaim);

        // استرداد ملف المريض الحالي وعرض بياناته الشخصية في لوحة التحكم لمراجعة الملف الطبي وعمليات الدفع
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        // جلب المواعيد المستقبلية القادمة للمريض لمتابعة التواريخ وسداد العربون للعيادة للقبول المبدئي من الطبيب
        var upcomingAppointments = await _context.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Branch)
            .Where(a => a.PatientId == patientId && a.AppointmentDateTime >= DateTime.Now && a.Status != "Cancelled" && a.Status != "Rejected")
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
        ViewBag.UpcomingAppointments = upcomingAppointments;

        // جلب سجل الزيارات السابقة للمريض (مكتمل، ملغى، مرفوض) للمراجعة التاريخية والاطلاع على الفحوصات الطبية للعيون والوصفات المعطاة
        var appointmentHistory = await _context.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Branch)
            .Where(a => a.PatientId == patientId && (a.AppointmentDateTime < DateTime.Now || a.Status == "Completed" || a.Status == "Rejected"))
            .OrderByDescending(a => a.AppointmentDateTime)
            .ToListAsync();
        ViewBag.AppointmentHistory = appointmentHistory;

        // جلب آخر 5 وصفات طبية (Rx) صادرة للمريض لمراجعة الأدوية الموصوفة والجرعات والتعليمات الطبية المحددة من الأطباء المعالجين بالعيادة
        var prescriptions = await _context.PrescriptionHeaders
            .Include(ph => ph.Examination).ThenInclude(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
            .Include(ph => ph.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .Where(ph => ph.Examination.Appointment.PatientId == patientId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(5)
            .ToListAsync();
        ViewBag.Prescriptions = prescriptions;

        // جلب الفواتير الصادرة للمريض لتتبع الوضع المالي للمدفوعات الطبية والعمليات والمدفوع جزئياً وكلياً أو المتبقي سداده للعيادة
        var invoices = await _context.Invoices
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.IssuedAt)
            .Take(5)
            .ToListAsync();
        ViewBag.Invoices = invoices;

        // جلب قائمة بالملفات الطبية والتقارير المرفوعة بواسطة المريض (الأشعة العينية، الفحوصات الخارجية، التقارير) لسهولة اطلاع الأطباء بالعيادة
        var medicalFiles = await _context.MedicalFiles
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.UploadedAt)
            .ToListAsync();
        ViewBag.MedicalFiles = medicalFiles;

        // جلب الإشعارات الأخيرة الخاصة بحساب المريض لمتابعة حالة المواعيد والموافقة عليها والتنبيهات الهامة بالدفع للأطباء بالعيادة
        var patientUserClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(patientUserClaim))
        {
            int patUserId = int.Parse(patientUserClaim);
            ViewBag.RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == patUserId)
                .OrderByDescending(n => n.SentAt)
                .Take(10)
                .ToListAsync();
        }

        return View(patient);
    }

    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> ReceptionistDashboard()
    {
        // جلب قائمة الانتظار الحالية والمباشرة لليوم الحالي لتنظيم دخول المرضى لعيادات الأطباء العيون بالترتيب المسجل بالاستقبال بالعيادة
        var queue = await _context.Queues
            .Include(q => q.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(q => q.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
            .Where(q => q.CheckInTime.HasValue && q.CheckInTime.Value.Date == DateTime.Today && q.Status != "Completed")
            .OrderBy(q => q.QueueNumber)
            .ToListAsync();
        ViewBag.ActiveQueue = queue;

        // جلب قائمة بمواعيد اليوم لمساعد الاستقبال على مراجعة الحضور وسداد العربون وتفعيل الدخول لقائمة الانتظار بالعيادة
        var todayApps = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Where(a => a.AppointmentDateTime.Date == DateTime.Today)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
        ViewBag.TodayAppointments = todayApps;

        // جلب كافة المواعيد الأخيرة المضافة لمراجعة الحجوزات السابقة والمستقبلية وتعديلها وإلغائها حسب الحاجة بالعيادة لموظف الاستقبال
        var allAppointments = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Branch)
            .OrderByDescending(a => a.AppointmentDateTime)
            .Take(30)
            .ToListAsync();
        ViewBag.AllAppointments = allAppointments;

        // جلب المواعيد التي تتطلب سداد دفعة مقدمة (العربون) لمراجعة وصول التحويلات وتأكيد الدفع المالي تمهيداً لإحالة الموعد للأطباء للقبول بالعيادة
        var deposits = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Where(a => a.DepositAmount > 0)
            .OrderByDescending(a => a.CreatedAt)
            .Take(15)
            .ToListAsync();
        ViewBag.RecentDeposits = deposits;

        // جلب دليل المرضى الأخير للبحث السريع وتحديث البيانات الشخصية للمريض أو الحجز المباشر بالعيادة لموظف الاستقبال لسهولة التنقل بالنظام
        var patients = await _context.Patients
            .Include(p => p.User)
            .Take(30)
            .ToListAsync();
        ViewBag.Patients = patients;

        // جلب قائمة الأطباء في النظام مع تخصصاتهم لتسهيل الرد على الاستفسارات وإرشاد المرضى بالعيادة لموظف الاستقبال ومحرك البحث للخدمة
        var doctors = await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Specialization)
            .ToListAsync();
        ViewBag.Doctors = doctors;

        // إحصائيات عامة للوحة التحكم لموظف الاستقبال لمتابعة أعداد الحضور والانتظار والفواتير غير المسددة اليوم بالعيادة للسيطرة الكاملة
        ViewBag.QueueTotal = await _context.Queues.CountAsync(q => q.CheckInTime.HasValue && q.CheckInTime.Value.Date == DateTime.Today);
        ViewBag.QueueWaiting = await _context.Queues.CountAsync(q => q.CheckInTime.HasValue && q.CheckInTime.Value.Date == DateTime.Today && q.Status == "Waiting");
        ViewBag.UnpaidInvoices = await _context.Invoices.CountAsync(i => i.Status == "Unpaid");

        // إشعارات موظف الاستقبال الأخيرة للتنبيه التلقائي عن المواعيد المنشأة من البوابة الخارجية أو عمليات السداد الجديدة للمرضى بالعيادة
        var recepUserClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(recepUserClaim))
        {
            int recepUserId = int.Parse(recepUserClaim);
            ViewBag.RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == recepUserId)
                .OrderByDescending(n => n.SentAt)
                .Take(10)
                .ToListAsync();
        }

        return View();
    }

    public IActionResult Profile()
    {
        return RedirectToAction("Profile", "Account");
    }

    public IActionResult PatientPortal()
    {
        return RedirectToAction("Index"); // إعادة توجيه المستخدم إلى لوحة التحكم المناسبة تبعا لدور حسابه بالعيادة لعدم التداخل بالروابط وتأمين الوصول للخدمات الكلية للنظام
    }

    [Authorize]
    public IActionResult Calendar()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult About()
    {
        return Redirect("/Home/Landing#about");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Contact()
    {
        return Redirect("/Home/Landing#contact");
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(string name, string email, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || 
            string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "All inquiry form fields are required.";
            return Redirect("/Home/Landing#contact");
        }

        TempData["Success"] = "Your inquiry has been submitted successfully! Our staff will contact you shortly.";
        return Redirect("/Home/Landing#contact");
    }

    [AllowAnonymous]
    public IActionResult Doctors()
    {
        return Redirect("/Home/Landing#doctors");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

