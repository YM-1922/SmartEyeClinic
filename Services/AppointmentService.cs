using System;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Services
{
    public class AppointmentService
    {
        private readonly AppDbContext _context;

        public AppointmentService(AppDbContext context)
        {
            _context = context;
        }

        public void AddAppointment()
        {
            Console.Write("Patient Id: ");
            int patientId = int.Parse(Console.ReadLine()!);

            Console.Write("Doctor Id: ");
            int doctorId = int.Parse(Console.ReadLine()!);

            Console.Write("Branch Id: ");
            int branchId = int.Parse(Console.ReadLine()!);

            var patientExists = _context.Patients.Any(p => p.Id == patientId);
            var doctorExists = _context.Doctors.Any(d => d.Id == doctorId);
            var branchExists = _context.Branches.Any(b => b.Id == branchId);

            if (!patientExists)
            {
                Console.WriteLine("Patient not found!");
                return;
            }

            if (!doctorExists)
            {
                Console.WriteLine("Doctor not found!");
                return;
            }

            if (!branchExists)
            {
                Console.WriteLine("Branch not found!");
                return;
            }

            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = doctorId,
                BranchId = branchId,
                AppointmentDateTime = DateTime.Now.AddDays(1),
                DurationMinutes = 30,
                Type = "Consultation",
                Status = "Scheduled",
                Notes = "First Visit",
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            _context.SaveChanges();

            Console.WriteLine("Appointment Added Successfully!");
        }

        public void ShowAppointments()
        {
            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Patient.User)
                .Include(a => a.Doctor)
                .Include(a => a.Doctor.User)
                .ToList();

            Console.WriteLine("\n===== Appointments =====");

            foreach (var a in appointments)
            {
                Console.WriteLine(
                    $"ID: {a.Id} | " +
                    $"Patient: {a.Patient.User.FullName} | " +
                    $"Doctor: {a.Doctor.User.FullName} | " +
                    $"Date: {a.AppointmentDateTime} | " +
                    $"Status: {a.Status}"
                );
            }
        }
    }
}
