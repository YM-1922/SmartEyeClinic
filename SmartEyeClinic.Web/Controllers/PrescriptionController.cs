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
    public class PrescriptionController : Controller
    {
        private readonly PrescriptionService _prescriptionService;
        private readonly AppDbContext _context;

        public PrescriptionController(PrescriptionService prescriptionService, AppDbContext context)
        {
            _prescriptionService = prescriptionService;
            _context = context;
        }

        // GET: /Prescription/Index | استعراض الوصفات الطبية الصادرة وتصفيتها للمرضى والأطباء
        public async Task<IActionResult> Index()
        {
            var prescriptions = await _prescriptionService.GetAllPrescriptionsAsync();

            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim != null)
                {
                    int patId = int.Parse(patIdClaim);
                    prescriptions = prescriptions.Where(p => p.PrescriptionHeader.Examination.Appointment.PatientId == patId).ToList();
                }
                else
                {
                    prescriptions = new List<PrescriptionItem>();
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim != null)
                {
                    int docId = int.Parse(docIdClaim);
                    prescriptions = prescriptions.Where(p => p.PrescriptionHeader.Examination.Appointment.DoctorId == docId).ToList();
                }
                else
                {
                    prescriptions = new List<PrescriptionItem>();
                }
            }

            return View(prescriptions);
        }

        // GET: /Prescription/Details/{id} | تفاصيل الوصفة الطبية (نموذج الطباعة الطبي Rx) مع حماية الهوية
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var rxHeader = await _prescriptionService.GetPrescriptionHeaderByIdAsync(id);
            if (rxHeader == null)
            {
                TempData["Error"] = "Prescription not found.";
                return RedirectToAction(nameof(Index));
            }

            // التحقق من الصلاحيات والوصول العشوائي
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != rxHeader.Examination.Appointment.PatientId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != rxHeader.Examination.Appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            return View(rxHeader);
        }

        // GET: /Prescription/Create | عرض صفحة إنشاء وصفة طبية جديدة للأطباء
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Create(int? examinationId = null)
        {
            await PopulateDropdownsAsync(examinationId);
            return View();
        }

        // POST: /Prescription/Create | معالجة إضافة وحفظ الأدوية والجرعات والتعليمات الطبية للمريض
        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int examinationId, int medicineId, string? dosage, int durationDays, string? instructions)
        {
            var result = await _prescriptionService.CreatePrescriptionAsync(examinationId, medicineId, dosage, durationDays, instructions);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await PopulateDropdownsAsync(examinationId);
                return View();
            }
            TempData["Success"] = "Prescription created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Prescription/Delete/{id} | عرض صفحة تأكيد حذف وصفة طبية للطبيب أو الإدارة
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var rx = await _prescriptionService.GetPrescriptionHeaderByIdAsync(id);
            if (rx == null)
            {
                TempData["Error"] = "Prescription not found.";
                return RedirectToAction(nameof(Index));
            }

            // التحقق الأمني: يمنع الحذف لغير المدير أو الطبيب كاتب الوصفة نفسه
            if (!User.IsInRole("Admin"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != rx.Examination.Appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            return View(rx);
        }

        // POST: /Prescription/Delete/{id} | حذف الوصفة الطبية نهائياً من قاعدة البيانات بعد التحقق من الهوية
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rx = await _prescriptionService.GetPrescriptionHeaderByIdAsync(id);
            if (rx == null)
            {
                TempData["Error"] = "Prescription not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsInRole("Admin"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != rx.Examination.Appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            var result = await _prescriptionService.DeletePrescriptionAsync(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Prescription deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // دالة مساعدة لتعبئة قوائم الفحوصات الطبية والأدوية في النماذج
        private async Task PopulateDropdownsAsync(int? selectedExamId = null)
        {
            IQueryable<Examination> query = _context.Examinations
                .Include(e => e.Appointment)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(p => p.User)
                .Include(e => e.Appointment)
                    .ThenInclude(a => a.Doctor)
                        .ThenInclude(d => d.User);

            if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim != null)
                {
                    int docId = int.Parse(docIdClaim);
                    query = query.Where(e => e.Appointment.DoctorId == docId);
                }
            }

            ViewBag.Examinations = await query.OrderByDescending(e => e.ExaminedAt).ToListAsync();
            ViewBag.Medicines = await _context.Medicines.AsNoTracking().OrderBy(m => m.Name).ToListAsync();
            ViewBag.SelectedExaminationId = selectedExamId;
        }
    }
}
