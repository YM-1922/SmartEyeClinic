using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Services
{
    public class PatientService
    {
        private readonly AppDbContext _context;

        public PatientService(AppDbContext context)
        {
            _context = context;
        }

        // Get All Patients
        public List<Patient> GetAllPatients()
        {
            return _context.Patients
                .Include(p => p.User)
                .ToList();
        }

        // Add Patient (interactive console mode)
        public void AddPatient()
        {
            var patientRole = _context.Roles.FirstOrDefault(r => r.Name == "Patient");
            if (patientRole == null)
            {
                Console.WriteLine("Patient role is not initialized in the database.");
                return;
            }

            Console.Write("Full Name: ");
            string fullName = Console.ReadLine()!;
            Console.Write("Email: ");
            string email = Console.ReadLine()!;
            Console.Write("Password: ");
            string password = Console.ReadLine()!;
            Console.Write("Phone Number: ");
            string phoneNumber = Console.ReadLine()!;

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = password,
                PhoneNumber = phoneNumber,
                RoleId = patientRole.Id,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            Console.Write("National ID: ");
            string nationalId = Console.ReadLine()!;
            Console.Write("Gender (Male/Female): ");
            string gender = Console.ReadLine()!;
            Console.Write("Address: ");
            string address = Console.ReadLine()!;
            Console.Write("Date of Birth (YYYY-MM-DD): ");
            string dobStr = Console.ReadLine()!;
            DateOnly.TryParse(dobStr, out DateOnly dob);
            if (dob == default) dob = new DateOnly(2000, 1, 1);

            var patient = new Patient
            {
                UserId = user.Id,
                NationalId = nationalId,
                Gender = gender,
                Address = address,
                DateOfBirth = dob
            };

            _context.Patients.Add(patient);
            _context.SaveChanges();

            Console.WriteLine("Patient Added Successfully!");
        }

        // Show Patients
        public void ShowPatients()
        {
            var patients = _context.Patients
                .Include(p => p.User)
                .ToList();

            Console.WriteLine("\n===== Patients =====");
            foreach (var p in patients)
            {
                Console.WriteLine("--------------------");
                Console.WriteLine($"Id = {p.Id}");
                Console.WriteLine($"Name = {p.User.FullName}");
                Console.WriteLine($"Email = {p.User.Email}");
                Console.WriteLine($"National ID = {p.NationalId}");
                Console.WriteLine($"Gender = {p.Gender}");
            }
        }
    }
}