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

    // جلب قائمة بجميع المرضى المسجلين في العيادة مع تفاصيل حساباتهم الشخصية
    public async Task<List<Patient>> GetAllPatientsAsync()
    {
        return await _context.Patients
            .Include(p => p.User)
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    // جلب ملف مريض معين باستخدام معرفه الفريد مع تفاصيل الحساب
    public async Task<Patient?> GetPatientByIdAsync(int id)
    {
        return await _context.Patients
            .Include(p => p.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // إضافة مريض جديد وإنشاء حساب مستخدم مرتبط به في النظام
    public async Task AddPatientAsync(
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

        bool exists = await _context.Patients.AnyAsync(p => p.NationalId == nationalId);

        if (exists)
            throw new Exception("This National ID already exists.");

        bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);

        if (phoneExists)
            throw new Exception("Phone Number already exists.");

        var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Patient");
        if (patientRole == null)
            throw new Exception("Patient role is not initialized in the database.");

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
        await _context.SaveChangesAsync();

        var patient = new Patient
        {
            UserId = user.Id,
            NationalId = nationalId,
            Gender = gender,
            Address = address,
            DateOfBirth = new DateOnly(2000, 1, 1)
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
    }

    // تحديث بيانات ملف المريض وتعديل بيانات حسابه الشخصي المرتبط
    public async Task UpdatePatientAsync(
        int id,
        string fullName,
        string email,
        string password,
        string phoneNumber,
        string nationalId,
        string gender,
        string address)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);

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

        // التحقق من عدم تكرار الرقم القومي لمريض آخر مسجل بالعيادة
        bool exists = await _context.Patients.AnyAsync(p => p.NationalId == nationalId && p.Id != id);
        if (exists)
            throw new Exception("This National ID already exists.");

        // التحقق من عدم استخدام رقم الهاتف من قبل مستخدم آخر في النظام
        bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber && u.Id != patient.UserId);
        if (phoneExists)
            throw new Exception("Phone Number already exists.");

        // التحقق من عدم استخدام البريد الإلكتروني من قبل مستخدم آخر في النظام
        bool emailExists = await _context.Users.AnyAsync(u => u.Email == email && u.Id != patient.UserId);
        if (emailExists)
            throw new Exception("Email already exists.");

        patient.User.FullName = fullName;
        patient.User.Email = email;
        patient.User.PasswordHash = password;
        patient.User.PhoneNumber = phoneNumber;

        patient.NationalId = nationalId;
        patient.Gender = gender;
        patient.Address = address;

        await _context.SaveChangesAsync();
    }

    // حذف ملف المريض وحسابه الشخصي بعد التأكد من عدم وجود أي ارتباطات طبية أو مالية نشطة له
    public async Task DeletePatientAsync(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
            throw new Exception("Patient not found.");

        var user = patient.User;

        // التحقق من كافة القيود والملفات والارتباطات النشطة للمريض (مواعيد، فواتير، ملفات طبية، مراجعات) قبل الحذف لحماية البيانات من التلف المعلق
        if (await _context.Appointments.AnyAsync(a => a.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated appointments.");

        if (await _context.Invoices.AnyAsync(i => i.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated invoices.");

        if (await _context.MedicalFiles.AnyAsync(m => m.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated medical files.");

        if (await _context.PatientHistories.AnyAsync(h => h.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated patient history.");

        if (await _context.PatientInsurances.AnyAsync(pi => pi.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated insurance information.");

        if (await _context.DoctorReviews.AnyAsync(r => r.PatientId == id))
            throw new Exception("Cannot delete patient because they have associated doctor reviews.");

        _context.Patients.Remove(patient);
        if (user != null)
        {
            _context.Users.Remove(user);
        }

        await _context.SaveChangesAsync();
    }
}