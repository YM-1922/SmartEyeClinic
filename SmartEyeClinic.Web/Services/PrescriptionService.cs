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
        public ServiceResult CreatePrescription(int examinationId, int medicineId,
            string? dosage, int durationDays, string? instructions)
        {
            var examinationExists = _context.Examinations.Any(e => e.Id == examinationId);
            var medicineExists    = _context.Medicines.Any(m => m.Id == medicineId);

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
            _context.SaveChanges();

            var prescriptionItem = new PrescriptionItem
            {
                PrescriptionId = prescriptionHeader.Id,
                MedicineId     = medicineId,
                Dosage         = dosage,
                DurationDays   = durationDays,
                Instructions   = instructions
            };

            _context.PrescriptionItems.Add(prescriptionItem);
            _context.SaveChanges();

            return ServiceResult.Ok();
        }

        // Get All Prescription Items (used for listing)
        public List<PrescriptionItem> GetAllPrescriptions()
        {
            return _context.PrescriptionItems
                .Include(p => p.PrescriptionHeader).ThenInclude(h => h.Examination).ThenInclude(e => e.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(p => p.PrescriptionHeader).ThenInclude(h => h.Examination).ThenInclude(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .Include(p => p.Medicine)
                .OrderByDescending(p => p.PrescriptionHeader.CreatedAt)
                .ToList();
        }

        // Get Prescription Header By ID (used for details sheet)
        public PrescriptionHeader? GetPrescriptionHeaderById(int id)
        {
            return _context.PrescriptionHeaders
                .Include(h => h.Examination).ThenInclude(e => e.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(h => h.Examination).ThenInclude(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .Include(h => h.PrescriptionItems).ThenInclude(i => i.Medicine)
                .FirstOrDefault(h => h.Id == id);
        }

        // Delete Prescription
        public ServiceResult DeletePrescription(int id)
        {
            var header = _context.PrescriptionHeaders.Find(id);
            if (header == null)
                return ServiceResult.Fail("Prescription not found.");

            // Remove associated prescription items first
            var items = _context.PrescriptionItems.Where(pi => pi.PrescriptionId == id);
            _context.PrescriptionItems.RemoveRange(items);

            _context.PrescriptionHeaders.Remove(header);
            _context.SaveChanges();

            return ServiceResult.Ok();
        }
    }
}
