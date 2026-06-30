using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services
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

        // Add Patient
        public void AddPatient(string nationalId, string gender, string address)
        {
            var user = new User
            {
                FullName = "Patient User",
                Email = $"patient{Guid.NewGuid()}@gmail.com",
                PasswordHash = "123",
                RoleId = 1,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            var patient = new Patient
            {
                UserId = user.Id,
                NationalId = nationalId,
                Gender = gender,
                Address = address,
                DateOfBirth = new DateOnly(2000, 1, 1)
            };

            _context.Patients.Add(patient);
            _context.SaveChanges();
        }

        // Get Patient By Id
        public Patient? GetPatient(int id)
        {
            return _context.Patients
                .Include(p => p.User)
                .FirstOrDefault(p => p.Id == id);
        }

        // Delete Patient
        public void DeletePatient(int id)
        {
            var patient = _context.Patients.FirstOrDefault(p => p.Id == id);

            if (patient == null)
                return;

            _context.Patients.Remove(patient);
            _context.SaveChanges();
        }
    }
}