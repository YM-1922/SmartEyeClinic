using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services
{
    public class DoctorService
    {
        private readonly AppDbContext _context;

        public DoctorService(AppDbContext context)
        {
            _context = context;
        }

        // Get All Doctors
        public List<Doctor> GetAllDoctors()
        {
            return _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .OrderByDescending(d => d.Id)
                .ToList();
        }

        // Get Doctor by Id
        public Doctor? GetDoctorById(int id)
        {
            return _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .FirstOrDefault(d => d.Id == id);
        }

        // Add Doctor
        public void AddDoctor(
            string fullName,
            string email,
            string password,
            string phoneNumber,
            int specializationId, 
            string licenseNumber, 
            decimal consultationFee, 
            string? bio)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new Exception("Full Name is required.");

            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("Email is required.");

            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password is required.");

            if (string.IsNullOrWhiteSpace(licenseNumber))
                throw new Exception("License Number is required.");

            bool licenseExists = _context.Doctors.Any(d => d.LicenseNumber == licenseNumber);
            if (licenseExists)
                throw new Exception("License Number already exists.");

            bool emailExists = _context.Users.Any(u => u.Email == email);
            if (emailExists)
                throw new Exception("Email already exists.");

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                bool phoneExists = _context.Users.Any(u => u.PhoneNumber == phoneNumber);
                if (phoneExists)
                    throw new Exception("Phone Number already exists.");
            }

            var doctorUser = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = password,
                PhoneNumber = phoneNumber,
                RoleId = 2, // Role 2 is Doctor
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(doctorUser);
            _context.SaveChanges();

            var doctor = new Doctor
            {
                UserId = doctorUser.Id,
                SpecializationId = specializationId,
                LicenseNumber = licenseNumber,
                ConsultationFee = consultationFee,
                Bio = bio
            };

            _context.Doctors.Add(doctor);
            _context.SaveChanges();
        }

        // Update Doctor
        public void UpdateDoctor(
            int id,
            string fullName,
            string email,
            string password,
            string phoneNumber,
            int specializationId,
            string licenseNumber,
            decimal consultationFee,
            string? bio)
        {
            var doctor = _context.Doctors
                .Include(d => d.User)
                .FirstOrDefault(d => d.Id == id);

            if (doctor == null)
                throw new Exception("Doctor profile not found.");

            if (string.IsNullOrWhiteSpace(fullName))
                throw new Exception("Full Name is required.");

            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("Email is required.");

            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Password is required.");

            if (string.IsNullOrWhiteSpace(licenseNumber))
                throw new Exception("License Number is required.");

            // Unique Checks
            bool licenseExists = _context.Doctors.Any(d => d.LicenseNumber == licenseNumber && d.Id != id);
            if (licenseExists)
                throw new Exception("License Number already exists.");

            bool emailExists = _context.Users.Any(u => u.Email == email && u.Id != doctor.UserId);
            if (emailExists)
                throw new Exception("Email already exists.");

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                bool phoneExists = _context.Users.Any(u => u.PhoneNumber == phoneNumber && u.Id != doctor.UserId);
                if (phoneExists)
                    throw new Exception("Phone Number already exists.");
            }

            doctor.User.FullName = fullName;
            doctor.User.Email = email;
            doctor.User.PasswordHash = password;
            doctor.User.PhoneNumber = phoneNumber;

            doctor.SpecializationId = specializationId;
            doctor.LicenseNumber = licenseNumber;
            doctor.ConsultationFee = consultationFee;
            doctor.Bio = bio;

            _context.SaveChanges();
        }

        // Delete Doctor
        public void DeleteDoctor(int id)
        {
            var doctor = _context.Doctors
                .Include(d => d.User)
                .FirstOrDefault(d => d.Id == id);

            if (doctor == null)
                throw new Exception("Doctor not found.");

            // Dependency constraint checks before deleting
            if (_context.Appointments.Any(a => a.DoctorId == id))
                throw new Exception("Cannot delete doctor because they have associated patient appointments.");

            if (_context.Surgeries.Any(s => s.DoctorId == id))
                throw new Exception("Cannot delete doctor because they have scheduled surgeries.");

            if (_context.DoctorSchedules.Any(ds => ds.DoctorId == id))
            {
                var schedules = _context.DoctorSchedules.Where(ds => ds.DoctorId == id);
                _context.DoctorSchedules.RemoveRange(schedules);
            }

            var user = doctor.User;

            _context.Doctors.Remove(doctor);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            _context.SaveChanges();
        }
    }
}
