using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services
{
    public class PrescriptionService
    {
        private readonly AppDbContext _context;

        public PrescriptionService(AppDbContext context)
        {
            _context = context;
        }

        // Create Prescription
        public async Task<ServiceResult> CreatePrescriptionAsync(int examinationId, int medicineId,
            string? dosage, int durationDays, string? instructions)
        {
            var examinationExists = await _context.Examinations.AnyAsync(e => e.Id == examinationId);
            var medicineExists    = await _context.Medicines.AnyAsync(m => m.Id == medicineId);

            if (!examinationExists)
                return ServiceResult.Fail("Examination not found!");

            if (!medicineExists)
                return ServiceResult.Fail("Medicine not found!");

            var prescriptionHeader = new PrescriptionHeader
            {
                ExaminationId = examinationId,
                CreatedAt     = DateTime.Now
            };

            _context.PrescriptionHeaders.Add(prescriptionHeader);
            await _context.SaveChangesAsync();

            var prescriptionItem = new PrescriptionItem
            {
                PrescriptionId = prescriptionHeader.Id,
                MedicineId     = medicineId,
                Dosage         = dosage,
                DurationDays   = durationDays,
                Instructions   = instructions
            };

            _context.PrescriptionItems.Add(prescriptionItem);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        // Get All Prescription Items (used for listing)
        public async Task<List<PrescriptionItem>> GetAllPrescriptionsAsync()
        {
            return await _context.PrescriptionItems
                .Include(p => p.PrescriptionHeader).ThenInclude(h => h.Examination).ThenInclude(e => e.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(p => p.PrescriptionHeader).ThenInclude(h => h.Examination).ThenInclude(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .Include(p => p.Medicine)
                .AsNoTracking()
                .OrderByDescending(p => p.PrescriptionHeader.CreatedAt)
                .ToListAsync();
        }

        // Get Prescription Header By ID (used for details sheet)
        public async Task<PrescriptionHeader?> GetPrescriptionHeaderByIdAsync(int id)
        {
            return await _context.PrescriptionHeaders
                .Include(h => h.Examination).ThenInclude(e => e.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(h => h.Examination).ThenInclude(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .Include(h => h.PrescriptionItems).ThenInclude(i => i.Medicine)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id);
        }

        // Delete Prescription
        public async Task<ServiceResult> DeletePrescriptionAsync(int id)
        {
            var header = await _context.PrescriptionHeaders.FindAsync(id);
            if (header == null)
                return ServiceResult.Fail("Prescription not found.");

            // Remove associated prescription items first
            var items = _context.PrescriptionItems.Where(pi => pi.PrescriptionId == id);
            _context.PrescriptionItems.RemoveRange(items);

            _context.PrescriptionHeaders.Remove(header);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok();
        }
    }
}
