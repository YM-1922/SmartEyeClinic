using System;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Services
{
    public class DoctorService
    {
        private readonly AppDbContext _context;

        public DoctorService(AppDbContext context)
        {
            _context = context;
        }

        public void AddDoctor()
        {
            var doctorUser = new User
            {
                FullName = $"Doctor {Guid.NewGuid()}",
                Email = $"doctor{Guid.NewGuid()}@gmail.com",
                PasswordHash = "123456",
                PhoneNumber = $"010{new Random().Next(10000000, 99999999)}",
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(doctorUser);
            _context.SaveChanges();

            var doctor = new Doctor();

            doctor.UserId = doctorUser.Id;

            Console.Write("Specialization Id: ");
            doctor.SpecializationId = int.Parse(Console.ReadLine()!);

            Console.Write("License Number: ");
            doctor.LicenseNumber = Console.ReadLine()!;

            Console.Write("Consultation Fee: ");
            doctor.ConsultationFee = decimal.Parse(Console.ReadLine()!);

            Console.Write("Bio: ");
            doctor.Bio = Console.ReadLine();

            Console.WriteLine("\n====== DEBUG ======");
            Console.WriteLine($"UserId = {doctor.UserId}");
            Console.WriteLine($"SpecializationId = {doctor.SpecializationId}");
            Console.WriteLine($"LicenseNumber = {doctor.LicenseNumber}");
            Console.WriteLine($"ConsultationFee = {doctor.ConsultationFee}");
            Console.WriteLine("===================\n");

            _context.Doctors.Add(doctor);
            _context.SaveChanges();

            Console.WriteLine("Doctor Added Successfully!");
        }

        public void ShowDoctors()
        {
            var doctors = _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .ToList();

            Console.WriteLine("\n===== Doctors =====");

            foreach (var d in doctors)
            {
                Console.WriteLine("--------------------");
                Console.WriteLine($"Id = {d.Id}");
                Console.WriteLine($"UserId = {d.UserId}");
                Console.WriteLine($"License = [{d.LicenseNumber}]");
                Console.WriteLine($"Fee = {d.ConsultationFee}");
                Console.WriteLine($"SpecializationId = {d.SpecializationId}");
            }
        }
    }
}
