using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Controllers
{
    [Authorize]
    public class SurgeryController : Controller
    {
        private readonly AppDbContext _context;

        public SurgeryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Surgery/Index | استعراض العمليات الجراحية الجارية وتصفيتها للمرضى والأطباء
        public async Task<IActionResult> Index()
        {
            var query = _context.Surgeries
                .Include(s => s.Patient).ThenInclude(p => p.User)
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.SurgeryType)
                .AsNoTracking()
                .AsQueryable();

            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null) return RedirectToAction("AccessDenied", "Account");
                int patId = int.Parse(patIdClaim);
                query = query.Where(s => s.PatientId == patId);
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null) return RedirectToAction("AccessDenied", "Account");
                int docId = int.Parse(docIdClaim);
                query = query.Where(s => s.DoctorId == docId);
            }

            var surgeries = await query.OrderByDescending(s => s.SurgeryDate).ToListAsync();
            return View(surgeries);
        }

        // GET: /Surgery/Details/{id} | عرض تفاصيل العملية الجراحية مع حماية البيانات من التلاعب (IDOR Protection)
        public async Task<IActionResult> Details(int id)
        {
            var surgery = await _context.Surgeries
                .Include(s => s.Patient).ThenInclude(p => p.User)
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.SurgeryType)
                .Include(s => s.Appointment)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (surgery == null)
                return NotFound();

            // التحقق من الصلاحيات لمنع التطفل والتلاعب ببيانات العمليات
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != surgery.PatientId)
                    return RedirectToAction("AccessDenied", "Account");
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != surgery.DoctorId)
                    return RedirectToAction("AccessDenied", "Account");
            }

            return View(surgery);
        }

        // GET: /Surgery/Create | عرض صفحة جدولة عملية جراحية جديدة (للأطباء والمدراء)
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Create(int? appointmentId = null)
        {
            await PopulateDropdownsAsync(appointmentId);
            return View();
        }

        // POST: /Surgery/Create | معالجة إضافة وجدولة العملية الجراحية الجديدة
        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Surgery surgery)
        {
            if (ModelState.IsValid)
            {
                _context.Surgeries.Add(surgery);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Surgery scheduled successfully!";
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropdownsAsync(surgery.AppointmentId);
            return View(surgery);
        }

        // GET: /Surgery/Edit/{id} | عرض صفحة تعديل بيانات عملية جراحية معينة للأطباء بعد تحقق الهوية
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var surgery = await _context.Surgeries.FindAsync(id);
            if (surgery == null)
                return NotFound();

            // التحقق الأمني: يمنع الطبيب من تعديل عمليات غيره
            if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != surgery.DoctorId)
                    return RedirectToAction("AccessDenied", "Account");
            }

            await PopulateDropdownsAsync(surgery.AppointmentId);
            return View(surgery);
        }

        // POST: /Surgery/Edit/{id} | معالجة تحديث بيانات العملية الجراحية
        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Surgery surgery)
        {
            if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != surgery.DoctorId)
                    return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                _context.Update(surgery);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Surgery record updated successfully!";
                return RedirectToAction(nameof(Details), new { id = surgery.Id });
            }
            await PopulateDropdownsAsync(surgery.AppointmentId);
            return View(surgery);
        }

        // GET: /Surgery/Delete/{id} | عرض صفحة تأكيد حذف العملية الجراحية للادارة
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var surgery = await _context.Surgeries
                .Include(s => s.Patient).ThenInclude(p => p.User)
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.SurgeryType)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (surgery == null)
                return NotFound();

            return View(surgery);
        }

        // POST: /Surgery/Delete/{id} | معالجة الحذف النهائي لجدول العملية الجراحية
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var surgery = await _context.Surgeries.FindAsync(id);
            if (surgery == null)
                return NotFound();

            _context.Surgeries.Remove(surgery);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Surgery schedule deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // دالة مساعدة لتعبئة قوائم المواعيد والمرضى والأطباء وأنواع العمليات
        private async Task PopulateDropdownsAsync(int? selectedAppId = null)
        {
            ViewBag.Appointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Where(a => a.Status != "Cancelled" || a.Id == selectedAppId)
                .AsNoTracking()
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            ViewBag.Patients = await _context.Patients
                .Include(p => p.User)
                .AsNoTracking()
                .OrderBy(p => p.User.FullName)
                .ToListAsync();

            ViewBag.Doctors = await _context.Doctors
                .Include(d => d.User)
                .AsNoTracking()
                .OrderBy(d => d.User.FullName)
                .ToListAsync();

            ViewBag.SurgeryTypes = await _context.SurgeryTypes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.SelectedAppointmentId = selectedAppId;
        }
    }
}
