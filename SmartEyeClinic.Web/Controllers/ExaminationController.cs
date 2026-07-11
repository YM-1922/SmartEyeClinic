using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;
using SmartEyeClinic.Web.Services;

namespace SmartEyeClinic.Web.Controllers
{
    [Authorize]
    public class ExaminationController : Controller
    {
        private readonly ExaminationService _examinationService;
        private readonly AppDbContext _context;

        public ExaminationController(ExaminationService examinationService, AppDbContext context)
        {
            _examinationService = examinationService;
            _context = context;
        }

        // GET: /Examination/Index | استعراض سجل الفحوصات الطبية وتصفيتها للمرضى والأطباء
        public async Task<IActionResult> Index()
        {
            var examinations = await _examinationService.GetAllExaminationsAsync();

            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim != null)
                {
                    int patId = int.Parse(patIdClaim);
                    examinations = examinations.Where(e => e.Appointment.PatientId == patId).ToList();
                }
                else
                {
                    examinations = new List<Examination>();
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim != null)
                {
                    int docId = int.Parse(docIdClaim);
                    examinations = examinations.Where(e => e.Appointment.DoctorId == docId).ToList();
                }
                else
                {
                    examinations = new List<Examination>();
                }
            }

            return View(examinations);
        }

        // GET: /Examination/Details/{id} | عرض تفاصيل فحص طبي معين (جدول حدة البصر وضغط العين)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var exam = await _examinationService.GetExaminationByIdAsync(id);
            if (exam == null)
            {
                TempData["Error"] = "Examination record not found.";
                return RedirectToAction(nameof(Index));
            }

            // التحقق من الصلاحيات والوصول لمنع التلاعب
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != exam.Appointment.PatientId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != exam.Appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            return View(exam);
        }

        // GET: /Examination/Create | عرض نموذج تسجيل فحص طبي جديد (للأطباء فقط)
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Create(int? appointmentId = null)
        {
            await PopulateAppointmentsDropdownAsync(appointmentId);
            return View();
        }

        // POST: /Examination/Create | تسجيل فحص طبي جديد وحفظ حدة البصر وتفاصيل التشخيص والخطة العلاجية
        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int appointmentId, 
            string diagnosis, 
            string? symptoms,
            string? visualAcuityLeft, 
            string? visualAcuityRight, 
            string? intraocularPressure, 
            string? treatmentPlan)
        {
            var result = await _examinationService.AddExaminationAsync(appointmentId, diagnosis, symptoms,
                visualAcuityLeft, visualAcuityRight, intraocularPressure, treatmentPlan);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await PopulateAppointmentsDropdownAsync(appointmentId);
                return View();
            }
            TempData["Success"] = "Examination record logged successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Examination/Edit/{id} | عرض صفحة تحرير فحص طبي معين للأطباء بعد تحقق الهوية
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var exam = await _examinationService.GetExaminationByIdAsync(id);
            if (exam == null)
                return NotFound();

            // التحقق من صلاحية الطبيب المعني لتعديل سجله الخاص فقط
            if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != exam.Appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            await PopulateAppointmentsDropdownAsync(exam.AppointmentId);
            return View(exam);
        }

        // POST: /Examination/Edit/{id} | تحديث بيانات فحص طبي معين وحفظ التعديلات
        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int appointmentId,
            string diagnosis,
            string? symptoms,
            string? visualAcuityLeft,
            string? visualAcuityRight,
            string? intraocularPressure,
            string? treatmentPlan)
        {
            var exam = await _examinationService.GetExaminationByIdAsync(id);
            if (exam == null)
                return NotFound();

            if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != exam.Appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            var result = await _examinationService.UpdateExaminationAsync(id, appointmentId, diagnosis, symptoms,
                visualAcuityLeft, visualAcuityRight, intraocularPressure, treatmentPlan);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await PopulateAppointmentsDropdownAsync(appointmentId);
                return View(exam);
            }
            TempData["Success"] = "Examination record updated successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Examination/Delete/{id} | عرض صفحة تأكيد حذف الفحص الطبي للادارة
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var exam = await _examinationService.GetExaminationByIdAsync(id);
            if (exam == null)
            {
                TempData["Error"] = "Examination record not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        // POST: /Examination/Delete/{id} | معالجة الحذف النهائي لسجل الفحص الطبي
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _examinationService.DeleteExaminationAsync(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Examination record deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // دالة مساعدة لتعبئة قائمة المواعيد المتاحة للفحص حسب الطبيب الجاري
        private async Task PopulateAppointmentsDropdownAsync(int? selectedId = null)
        {
            var query = _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Where(a => a.Status != "Cancelled" || a.Id == selectedId);

            if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim != null)
                {
                    int docId = int.Parse(docIdClaim);
                    query = query.Where(a => a.DoctorId == docId);
                }
            }

            ViewBag.Appointments = await query.OrderByDescending(a => a.AppointmentDateTime).ToListAsync();
            ViewBag.SelectedAppointmentId = selectedId;
        }
    }
}
