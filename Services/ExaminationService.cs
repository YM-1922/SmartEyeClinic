using System;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Services
{
    public class ExaminationService
    {
        private readonly AppDbContext _context;

        public ExaminationService(AppDbContext context)
        {
            _context = context;
        }

        public void AddExamination()
        {
            Console.Write("Appointment Id: ");
            int appointmentId = int.Parse(Console.ReadLine()!);

            var appointmentExists = _context.Appointments
                .Any(a => a.Id == appointmentId);

            if (!appointmentExists)
            {
                Console.WriteLine("Appointment not found!");
                return;
            }

            var examination = new Examination();

            examination.AppointmentId = appointmentId;

            Console.Write("Diagnosis: ");
            examination.Diagnosis = Console.ReadLine()!;

            Console.Write("Symptoms: ");
            examination.Symptoms = Console.ReadLine();

            Console.Write("Visual Acuity Left: ");
            examination.VisualAcuityLeft = Console.ReadLine();

            Console.Write("Visual Acuity Right: ");
            examination.VisualAcuityRight = Console.ReadLine();

            Console.Write("Intraocular Pressure: ");
            examination.IntraocularPressure = Console.ReadLine();

            Console.Write("Treatment Plan: ");
            examination.TreatmentPlan = Console.ReadLine();

            examination.ExaminedAt = DateTime.Now;

            _context.Examinations.Add(examination);
            _context.SaveChanges();

            Console.WriteLine("Examination Added Successfully!");
        }

        public void ShowExaminations()
        {
            var examinations = _context.Examinations
                .Include(e => e.Appointment)
                .ThenInclude(a => a.Patient)
                .ThenInclude(p => p.User)
                .Include(e => e.Appointment)
                .ThenInclude(a => a.Doctor)
                .ThenInclude(d => d.User)
                .ToList();

            Console.WriteLine("\n===== Examinations =====");

            foreach (var e in examinations)
            {
                Console.WriteLine(
                    $"ID: {e.Id} | " +
                    $"Patient: {e.Appointment.Patient.User.FullName} | " +
                    $"Doctor: {e.Appointment.Doctor.User.FullName} | " +
                    $"Diagnosis: {e.Diagnosis}"
                );
            }
        }
    }
}
