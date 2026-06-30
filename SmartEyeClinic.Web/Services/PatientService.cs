using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services;

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
            .OrderByDescending(p => p.Id)
            .ToList();
    }

    // Get Patient By Id
    public Patient? GetPatientById(int id)
    {
        return _context.Patients
            .Include(p => p.User)
            .FirstOrDefault(p => p.Id == id);
    }

    // Add Patient
    public void AddPatient(
        string? fullName,
        string? email,
        string? password,
        string? phoneNumber,
        string? nationalId,
        string? gender,
        string? address)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new Exception("Full Name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("Email is required.");

        if (string.IsNullOrWhiteSpace(password))
            throw new Exception("Password is required.");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new Exception("Phone Number is required.");

        if (string.IsNullOrWhiteSpace(nationalId))
            throw new Exception("National ID is required.");

        bool exists = _context.Patients.Any(p => p.NationalId == nationalId);

        if (exists)
            throw new Exception("This National ID already exists.");

        bool phoneExists = _context.Users.Any(u => u.PhoneNumber == phoneNumber);

        if (phoneExists)
            throw new Exception("Phone Number already exists.");

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = password,
            PhoneNumber = phoneNumber,
            RoleId = 3,
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

    // Update Patient
    public void UpdatePatient(
        int id,
        string fullName,
        string email,
        string password,
        string phoneNumber,
        string nationalId,
        string gender,
        string address)
    {
        var patient = _context.Patients
            .Include(p => p.User)
            .FirstOrDefault(p => p.Id == id);

        if (patient == null)
            throw new Exception("Patient not found.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new Exception("Full Name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("Email is required.");

        if (string.IsNullOrWhiteSpace(password))
            throw new Exception("Password is required.");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new Exception("Phone Number is required.");

        if (string.IsNullOrWhiteSpace(nationalId))
            throw new Exception("National ID is required.");

        // Check if national ID exists on another patient
        bool exists = _context.Patients.Any(p => p.NationalId == nationalId && p.Id != id);
        if (exists)
            throw new Exception("This National ID already exists.");

        // Check if phone number exists on another user
        bool phoneExists = _context.Users.Any(u => u.PhoneNumber == phoneNumber && u.Id != patient.UserId);
        if (phoneExists)
            throw new Exception("Phone Number already exists.");

        // Check if email exists on another user
        bool emailExists = _context.Users.Any(u => u.Email == email && u.Id != patient.UserId);
        if (emailExists)
            throw new Exception("Email already exists.");

        patient.User.FullName = fullName;
        patient.User.Email = email;
        patient.User.PasswordHash = password;
        patient.User.PhoneNumber = phoneNumber;

        patient.NationalId = nationalId;
        patient.Gender = gender;
        patient.Address = address;

        _context.SaveChanges();
    }

    // Delete Patient
    public void DeletePatient(int id)
    {
        var patient = _context.Patients
            .Include(p => p.User)
            .FirstOrDefault(p => p.Id == id);

        if (patient == null)
            throw new Exception("Patient not found.");

        var user = patient.User;

        // Check if patient has any dependent records that will break deletion
        if (_context.Appointments.Any(a => a.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated appointments.");

        if (_context.Invoices.Any(i => i.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated invoices.");
if (false)
    throw new Exception("Cannot delete patient because they have associated surgeries.");

        if (_context.MedicalFiles.Any(m => m.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated medical files.");

        if (_context.PatientHistories.Any(h => h.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated patient history.");

        if (_context.PatientInsurances.Any(pi => pi.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated insurance information.");

        if (_context.DoctorReviews.Any(r => r.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated doctor reviews.");

        _context.Patients.Remove(patient);
        if (user != null)
        {
            _context.Users.Remove(user);
        }

        _context.SaveChanges();
    }
}