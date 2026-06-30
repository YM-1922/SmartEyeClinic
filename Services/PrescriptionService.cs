using System;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Services
{
    public class PrescriptionService
    {
        private readonly AppDbContext _context;

        public PrescriptionService(AppDbContext context)
        {
            _context = context;
        }

        public void CreatePrescription()
        {
            Console.Write("Examination Id: ");
            int examinationId = int.Parse(Console.ReadLine()!);

            Console.Write("Medicine Id: ");
            int medicineId = int.Parse(Console.ReadLine()!);

            var examinationExists = _context.Examinations
                .Any(e => e.Id == examinationId);

            var medicineExists = _context.Medicines
                .Any(m => m.Id == medicineId);

            if (!examinationExists)
            {
                Console.WriteLine("Examination not found!");
                return;
            }

            if (!medicineExists)
            {
                Console.WriteLine("Medicine not found!");
                return;
            }

            var prescriptionHeader = new PrescriptionHeader
            {
                ExaminationId = examinationId,
                CreatedAt = DateTime.Now
            };

            _context.PrescriptionHeaders.Add(prescriptionHeader);
            _context.SaveChanges();

            var prescriptionItem = new PrescriptionItem();

            prescriptionItem.PrescriptionId = prescriptionHeader.Id;
            prescriptionItem.MedicineId = medicineId;

            Console.Write("Dosage: ");
            prescriptionItem.Dosage = Console.ReadLine();

            Console.Write("Duration Days: ");
            prescriptionItem.DurationDays = int.Parse(Console.ReadLine()!);

            Console.Write("Instructions: ");
            prescriptionItem.Instructions = Console.ReadLine();

            _context.PrescriptionItems.Add(prescriptionItem);
            _context.SaveChanges();

            Console.WriteLine("Prescription Created Successfully!");
        }

        public void ShowPrescriptions()
        {
            var prescriptions = _context.PrescriptionItems
                .Include(p => p.PrescriptionHeader)
                .ThenInclude(h => h.Examination)
                .ThenInclude(e => e.Appointment)
                .ThenInclude(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(p => p.PrescriptionHeader)
                .ThenInclude(h => h.Examination)
                .ThenInclude(e => e.Appointment)
                .ThenInclude(a => a.Doctor)
                .ThenInclude(d => d.User)
                .Include(p => p.Medicine)
                .ToList();

            Console.WriteLine("\n===== Prescriptions =====");

            foreach (var p in prescriptions)
            {
                Console.WriteLine(
                    $"Patient: {p.PrescriptionHeader.Examination.Appointment.Patient.User.FullName} | " +
                    $"Doctor: {p.PrescriptionHeader.Examination.Appointment.Doctor.User.FullName} | " +
                    $"Medicine: {p.Medicine.Name} | " +
                    $"Dosage: {p.Dosage} | " +
                    $"Duration: {p.DurationDays} Days"
                );
            }
        }
    }
}
