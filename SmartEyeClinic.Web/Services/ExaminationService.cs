using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services
{
    public class ExaminationService
    {
        private readonly AppDbContext _context;

        public ExaminationService(AppDbContext context)
        {
            _context = context;
        }

        // جلب كافة الفحوصات الطبية المسجلة بالنظام مع تفاصيل الموعد والطبيب والمريض
        public async Task<List<Examination>> GetAllExaminationsAsync()
        {
            return await _context.Examinations
                .Include(e => e.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .AsNoTracking()
                .OrderByDescending(e => e.ExaminedAt)
                .ToListAsync();
        }

        // جلب تفاصيل فحص طبي معين باستخدام معرفه الفريد
        public async Task<Examination?> GetExaminationByIdAsync(int id)
        {
            return await _context.Examinations
                .Include(e => e.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        // إضافة فحص طبي جديد للمريض وتحديث حالة الموعد تلقائياً
        public async Task<ServiceResult> AddExaminationAsync(
            int appointmentId, 
            string diagnosis, 
            string? symptoms,
            string? visualAcuityLeft, 
            string? visualAcuityRight, 
            string? intraocularPressure, 
            string? treatmentPlan)
        {
            if (string.IsNullOrWhiteSpace(diagnosis))
                return ServiceResult.Fail("Diagnosis description is required.");

            if (!await _context.Appointments.AnyAsync(a => a.Id == appointmentId))
                return ServiceResult.Fail("Appointment not found!");

            var examination = new Examination
            {
                AppointmentId = appointmentId,
                Diagnosis = diagnosis,
                Symptoms = symptoms,
                VisualAcuityLeft = visualAcuityLeft,
                VisualAcuityRight = visualAcuityRight,
                IntraocularPressure = intraocularPressure,
                TreatmentPlan = treatmentPlan,
                ExaminedAt = DateTime.Now
            };

            _context.Examinations.Add(examination);

            // تغيير حالة الموعد المرتبط تلقائياً إلى 'Completed' عند إجراء الفحص الطبي
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                appointment.Status = "Completed";
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        // تحديث بيانات فحص طبي مسجل مسبقاً في النظام
        public async Task<ServiceResult> UpdateExaminationAsync(
            int id,
            int appointmentId,
            string diagnosis,
            string? symptoms,
            string? visualAcuityLeft,
            string? visualAcuityRight,
            string? intraocularPressure,
            string? treatmentPlan)
        {
            var exam = await _context.Examinations.FindAsync(id);
            if (exam == null)
                return ServiceResult.Fail("Examination record not found.");

            if (string.IsNullOrWhiteSpace(diagnosis))
                return ServiceResult.Fail("Diagnosis description is required.");

            if (!await _context.Appointments.AnyAsync(a => a.Id == appointmentId))
                return ServiceResult.Fail("Appointment not found!");

            exam.AppointmentId = appointmentId;
            exam.Diagnosis = diagnosis;
            exam.Symptoms = symptoms;
            exam.VisualAcuityLeft = visualAcuityLeft;
            exam.VisualAcuityRight = visualAcuityRight;
            exam.IntraocularPressure = intraocularPressure;
            exam.TreatmentPlan = treatmentPlan;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        // حذف سجل الفحص الطبي بعد التأكد من عدم وجود وصفات طبية مرتبطة به
        public async Task<ServiceResult> DeleteExaminationAsync(int id)
        {
            var exam = await _context.Examinations.FindAsync(id);
            if (exam == null)
                return ServiceResult.Fail("Examination record not found.");

            // التحقق من وجود وصفات طبية صادرة بناءً على هذا الفحص لمنع الحذف العشوائي وتلف البيانات
            if (await _context.PrescriptionHeaders.AnyAsync(ph => ph.ExaminationId == id))
                return ServiceResult.Fail("Cannot delete this examination because prescriptions have been issued based on it.");

            _context.Examinations.Remove(exam);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok();
        }
    }
}
