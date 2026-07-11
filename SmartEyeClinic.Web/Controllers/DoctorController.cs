using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;
using SmartEyeClinic.Web.Services;

namespace SmartEyeClinic.Web.Controllers
{
    [Authorize]
    public class DoctorController : Controller
    {
        private readonly DoctorService _doctorService;
        private readonly AppDbContext _context;

        public DoctorController(DoctorService doctorService, AppDbContext context)
        {
            _doctorService = doctorService;
            _context = context;
        }

        // GET: /Doctor/Index | عرض قائمة الأطباء في النظام
        public async Task<IActionResult> Index()
        {
            var doctors = await _doctorService.GetAllDoctorsAsync();
            return View(doctors);
        }

        // GET: /Doctor/Details/{id} | عرض ملف الطبيب مع المراجعات والردود (ملف 360 درجة)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .Include(d => d.Reviews).ThenInclude(r => r.Patient).ThenInclude(p => p.User)
                .Include(d => d.Reviews).ThenInclude(r => r.Replies).ThenInclude(rep => rep.User).ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
            {
                TempData["Error"] = "Doctor profile not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(doctor);
        }

        // GET: /Doctor/Create | عرض نموذج تسجيل طبيب جديد وجدولة العمل للادارة
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Specializations = await _context.Specializations.AsNoTracking().ToListAsync();
            return View();
        }

        // POST: /Doctor/Create | معالجة تسجيل طبيب جديد، التحقق من عدم التكرار، رفع الصورة، وبذور الجدولة
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string fullName,
            string email,
            string password,
            string phoneNumber,
            int specializationId, 
            string licenseNumber, 
            decimal consultationFee, 
            string? bio,
            IFormFile? profilePicture,
            List<string>? workingDays,
            string? startTime,
            string? endTime)
        {
            try
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

                string? picturePath = null;
                if (profilePicture != null && profilePicture.Length > 0)
                {
                    var ext = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
                    var allowedExts = new[] { ".jpg", ".jpeg", ".png" };
                    if (!allowedExts.Contains(ext))
                        throw new Exception("Invalid image type. Only JPG, JPEG, and PNG are allowed.");

                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    var filename = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadDir, filename);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }
                    picturePath = $"/uploads/{filename}";
                }

                var doctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Doctor");
                if (doctorRole == null)
                    throw new Exception("Doctor role is not initialized in the database.");

                var user = new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = password,
                    PhoneNumber = phoneNumber,
                    RoleId = doctorRole.Id,
                    ProfilePicture = picturePath,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var doctor = new Doctor
                {
                    UserId = user.Id,
                    SpecializationId = specializationId,
                    LicenseNumber = licenseNumber,
                    ConsultationFee = consultationFee,
                    Bio = bio
                };
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                if (workingDays != null && workingDays.Any())
                {
                    TimeOnly start = TimeOnly.Parse(startTime ?? "09:00");
                    TimeOnly end = TimeOnly.Parse(endTime ?? "17:00");

                    foreach (var day in workingDays)
                    {
                        var schedule = new DoctorSchedule
                        {
                            DoctorId = doctor.Id,
                            DayOfWeek = day,
                            StartTime = start,
                            EndTime = end
                        };
                        _context.DoctorSchedules.Add(schedule);
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Doctor registered successfully with schedules!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.Specializations = await _context.Specializations.AsNoTracking().ToListAsync();
                return View();
            }
        }

        // GET: /Doctor/Edit/{id} | عرض نموذج تعديل بيانات الطبيب
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
                return NotFound();

            // التحقق من الهوية والأمن: تعديل البيانات مسموح للمدير أو الطبيب المعني فقط
            if (!User.IsInRole("Admin"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != id)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            ViewBag.Specializations = await _context.Specializations.AsNoTracking().ToListAsync();
            return View(doctor);
        }

        // POST: /Doctor/Edit/{id} | معالجة تعديل بيانات الطبيب بعد التحقق من الهوية
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
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
            if (!User.IsInRole("Admin"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != id)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            try
            {
                await _doctorService.UpdateDoctorAsync(id, fullName, email, password, phoneNumber, specializationId, licenseNumber, consultationFee, bio);
                TempData["Success"] = "Doctor updated successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.Specializations = await _context.Specializations.AsNoTracking().ToListAsync();
                var doctor = await _doctorService.GetDoctorByIdAsync(id);
                return View(doctor);
            }
        }

        // GET: /Doctor/Delete/{id} | عرض صفحة تأكيد حذف الطبيب
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
            {
                TempData["Error"] = "Doctor not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(doctor);
        }

        // POST: /Doctor/Delete/{id} | حذف حساب الطبيب نهائياً من قاعدة البيانات
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _doctorService.DeleteDoctorAsync(id);
                TempData["Success"] = "Doctor profile deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctor/AddReview | إضافة تقييم ومراجعة من مريض للطبيب
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int doctorId, int rating, string? comment)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Challenge();

            int userId = int.Parse(userIdClaim.Value);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient == null)
            {
                TempData["Error"] = "Only registered patients can submit reviews.";
                return RedirectToAction(nameof(Details), new { id = doctorId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5 stars.";
                return RedirectToAction(nameof(Details), new { id = doctorId });
            }

            var review = new DoctorReview
            {
                DoctorId = doctorId,
                PatientId = patient.Id,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.DoctorReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thank you for your feedback! Review submitted successfully.";
            return RedirectToAction(nameof(Details), new { id = doctorId });
        }

        // POST: /Doctor/AddReply | إضافة رد على مراجعة المريض من قبل الطبيب المعني أو المدير
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReply(int reviewId, int doctorId, string content)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Challenge();

            int userId = int.Parse(userIdClaim.Value);

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Reply content cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = doctorId });
            }

            // التحقق الأمني: السماح للمدير أو الطبيب المعني فقط بالرد
            bool isAuthorized = User.IsInRole("Admin");
            if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim != null && int.Parse(docIdClaim) == doctorId)
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                TempData["Error"] = "You are not authorized to reply to this review.";
                return RedirectToAction(nameof(Details), new { id = doctorId });
            }

            var reply = new ReviewReply
            {
                ReviewId = reviewId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.Now
            };

            _context.ReviewReplies.Add(reply);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reply posted successfully!";
            return RedirectToAction(nameof(Details), new { id = doctorId });
        }
    }
}
