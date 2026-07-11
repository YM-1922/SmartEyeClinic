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

        // جلب قائمة بجميع الأطباء المسجلين في النظام مع تفاصيل حساباتهم وتخصصاتهم
        public async Task<List<Doctor>> GetAllDoctorsAsync()
        {
            return await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .AsNoTracking()
                .OrderByDescending(d => d.Id)
                .ToListAsync();
        }

        // جلب بيانات طبيب معين باستخدام المعرف الفريد الخاص به
        public async Task<Doctor?> GetDoctorByIdAsync(int id)
        {
            return await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        // إضافة طبيب جديد وإنشاء حساب مستخدم مرتبط به في النظام
        public async Task AddDoctorAsync(
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

            bool licenseExists = await _context.Doctors.AnyAsync(d => d.LicenseNumber == licenseNumber);
            if (licenseExists)
                throw new Exception("License Number already exists.");

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
                throw new Exception("Email already exists.");

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
                if (phoneExists)
                    throw new Exception("Phone Number already exists.");
            }

            var doctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Doctor");
            if (doctorRole == null)
                throw new Exception("Doctor role is not initialized in the database.");

            var doctorUser = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = password,
                PhoneNumber = phoneNumber,
                RoleId = doctorRole.Id,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(doctorUser);
            await _context.SaveChangesAsync();

            var doctor = new Doctor
            {
                UserId = doctorUser.Id,
                SpecializationId = specializationId,
                LicenseNumber = licenseNumber,
                ConsultationFee = consultationFee,
                Bio = bio
            };

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();
        }

        // تحديث بيانات ملف الطبيب وبيانات حسابه الشخصي المرتبط
        public async Task UpdateDoctorAsync(
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
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

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

            // التحقق من عدم تكرار البريد الإلكتروني ورقم الهاتف ورقم الرخصة لطبيب آخر
            bool licenseExists = await _context.Doctors.AnyAsync(d => d.LicenseNumber == licenseNumber && d.Id != id);
            if (licenseExists)
                throw new Exception("License Number already exists.");

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == email && u.Id != doctor.UserId);
            if (emailExists)
                throw new Exception("Email already exists.");

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber && u.Id != doctor.UserId);
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

            await _context.SaveChangesAsync();
        }

        // حذف طبيب من النظام وحذف حسابه المرتبط به بعد التأكد من خلوه من أي ارتباطات نشطة
        public async Task DeleteDoctorAsync(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                throw new Exception("Doctor not found.");

            // التحقق من القيود والارتباطات (مواعيد أو عمليات مجدولة) لمنع حذف طبيب نشط
            if (await _context.Appointments.AnyAsync(a => a.DoctorId == id))
                throw new Exception("Cannot delete doctor because they have associated patient appointments.");

            if (await _context.Surgeries.AnyAsync(s => s.DoctorId == id))
                throw new Exception("Cannot delete doctor because they have scheduled surgeries.");

            if (await _context.DoctorSchedules.AnyAsync(ds => ds.DoctorId == id))
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

            await _context.SaveChangesAsync();
        }
    }
}
