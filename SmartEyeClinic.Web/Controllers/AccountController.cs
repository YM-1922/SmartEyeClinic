using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login | عرض صفحة تسجيل الدخول
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login | معالجة طلب تسجيل الدخول والتحقق من الحساب
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                return View();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null || user.PasswordHash != password) // مقارنة بكلمة المرور النصية البسيطة المتوافقة مع بذور البيانات الافتراضية
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View();
            }

            // إنشاء قائمة الادعاءات (Claims) لتخزين بيانات هوية المستخدم في الكوكيز
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            // جلب معرف الملف الشخصي بناءً على الدور وتخزينه كادعاء ليسهل التحقق منه لاحقاً
            if (user.Role.Name == "Doctor")
            {
                var doc = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                if (doc != null)
                {
                    claims.Add(new Claim("DoctorId", doc.Id.ToString()));
                }
            }
            else if (user.Role.Name == "Patient")
            {
                var pat = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (pat != null)
                {
                    claims.Add(new Claim("PatientId", pat.Id.ToString()));
                }
            }
            else if (user.Role.Name == "Receptionist")
            {
                var recep = await _context.Receptionists.FirstOrDefaultAsync(r => r.UserId == user.Id);
                if (recep != null)
                {
                    claims.Add(new Claim("ReceptionistId", recep.Id.ToString()));
                }
            }

            // إضافة صورة الملف الشخصي للادعاءات إن وجدت
            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                claims.Add(new Claim("ProfilePicture", user.ProfilePicture));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register | عرض صفحة إنشاء حساب جديد للمريض
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Register | معالجة طلب تسجيل مريض جديد والتحقق من فرادة البيانات
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password, string phoneNumber, string nationalId, string gender, string address, DateOnly dob)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(nationalId))
            {
                ModelState.AddModelError(string.Empty, "All fields marked with * are required.");
                return View();
            }

            // التحقق من عدم تكرار البريد الإلكتروني
            var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                ModelState.AddModelError("email", "This email address is already registered.");
                return View();
            }

            // التحقق من عدم تكرار رقم الهاتف
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
                if (phoneExists)
                {
                    ModelState.AddModelError("phoneNumber", "This phone number is already registered.");
                    return View();
                }
            }

            // التحقق من عدم تكرار الرقم القومي
            var nidExists = await _context.Patients.AnyAsync(p => p.NationalId == nationalId);
            if (nidExists)
            {
                ModelState.AddModelError("nationalId", "This National ID is already registered.");
                return View();
            }

            // الحصول على صلاحية المريض من قاعدة البيانات
            var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Patient");
            if (patientRole == null)
            {
                ModelState.AddModelError(string.Empty, "Patient role is not initialized in the database.");
                return View();
            }

            // إنشاء كائن مستخدم جديد
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

            // إنشاء الملف الشخصي للمريض
            var patient = new Patient
            {
                UserId = user.Id,
                NationalId = nationalId,
                Gender = gender,
                Address = address,
                DateOfBirth = dob
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Registration successful! Please login.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/Logout | تسجيل الخروج وإبطال جلسة الكوكيز
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/AccessDenied | عرض صفحة رفض الوصول عند عدم كفاية الصلاحيات
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/Profile | عرض صفحة الملف الشخصي للمستخدم الحالي
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Doctor).ThenInclude(d => d!.Specialization)
                .Include(u => u.Patient)
                .Include(u => u.Receptionist).ThenInclude(r => r!.Branch)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            return View(user);
        }

        // POST: /Account/EditProfile | تعديل بيانات الملف الشخصي
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(string fullName, string phoneNumber, string? address)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users
                .Include(u => u.Patient)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "Full name is required.";
                return RedirectToAction(nameof(Profile));
            }

            // التحقق من عدم تكرار رقم الهاتف المحدث
            if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber != user.PhoneNumber)
            {
                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber && u.Id != userId);
                if (phoneExists)
                {
                    TempData["Error"] = "This phone number is already registered.";
                    return RedirectToAction(nameof(Profile));
                }
            }

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.UpdatedAt = DateTime.Now;

            if (user.Patient != null)
            {
                user.Patient.Address = address;
            }

            await _context.SaveChangesAsync();
            
            // إعادة تسجيل الدخول لتحديث ادعاءات الكوكيز بالبيانات الجديدة
            var identity = (ClaimsIdentity)User.Identity!;
            var nameClaim = identity.FindFirst(ClaimTypes.Name);
            if (nameClaim != null) identity.RemoveClaim(nameClaim);
            identity.AddClaim(new Claim(ClaimTypes.Name, fullName));

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/ChangePassword | تغيير كلمة المرور للمستخدم
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New password and confirmation password do not match.";
                return RedirectToAction(nameof(Profile));
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            if (user.PasswordHash != currentPassword)
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Profile));
            }

            user.PasswordHash = newPassword;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(Microsoft.AspNetCore.Http.IFormFile? file)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid image file.";
                return RedirectToAction(nameof(Profile));
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Allowed formats: .jpg, .jpeg, .png, .webp, .gif";
                return RedirectToAction(nameof(Profile));
            }

            if (file.Length > 4 * 1024 * 1024)
            {
                TempData["Error"] = "Profile picture size must be less than 4MB.";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                if (!string.IsNullOrEmpty(user.ProfilePicture) && user.ProfilePicture.StartsWith("/uploads/avatars/"))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                var fileName = $"avatar_{userId}_{DateTime.UtcNow.Ticks}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                user.ProfilePicture = $"/uploads/avatars/{fileName}";
                user.UpdatedAt = DateTime.Now;
                
                // تحميل الكائن المرتبط لضمان سلامة الادعاءات المحدثة
                await _context.Entry(user).Reference(u => u.Role).LoadAsync();
                
                await _context.SaveChangesAsync();
                await RefreshUserClaimsAsync(user);

                TempData["Success"] = "Profile picture updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfilePicture()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction(nameof(Login));

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            try
            {
                if (!string.IsNullOrEmpty(user.ProfilePicture) && user.ProfilePicture.StartsWith("/uploads/avatars/"))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                user.ProfilePicture = null;
                user.UpdatedAt = DateTime.Now;
                
                // تحميل الدور لتحديث الكوكيز
                await _context.Entry(user).Reference(u => u.Role).LoadAsync();
                
                await _context.SaveChangesAsync();
                await RefreshUserClaimsAsync(user);

                TempData["Success"] = "Profile picture deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction(nameof(Profile));
        }

        // دالة مساعدة لإعادة بناء وتحديث ادعاءات المستخدم الحالي في الكوكيز
        private async Task RefreshUserClaimsAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "Patient")
            };

            if (user.Role?.Name == "Doctor")
            {
                var doc = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
                if (doc != null) claims.Add(new Claim("DoctorId", doc.Id.ToString()));
            }
            else if (user.Role?.Name == "Patient")
            {
                var pat = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (pat != null) claims.Add(new Claim("PatientId", pat.Id.ToString()));
            }
            else if (user.Role?.Name == "Receptionist")
            {
                var recep = await _context.Receptionists.FirstOrDefaultAsync(r => r.UserId == user.Id);
                if (recep != null) claims.Add(new Claim("ReceptionistId", recep.Id.ToString()));
            }

            if (!string.IsNullOrEmpty(user.ProfilePicture))
            {
                claims.Add(new Claim("ProfilePicture", user.ProfilePicture));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        // GET: /Account/CheckEmail
        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { available = false, message = "Email is required." });
            }

            var exists = await _context.Users.AnyAsync(u => u.Email == email);
            if (exists)
            {
                return Json(new { available = false, message = "This email address is already registered." });
            }

            return Json(new { available = true });
        }

        // GET: /Account/CheckNationalId
        [HttpGet]
        public async Task<IActionResult> CheckNationalId(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId))
            {
                return Json(new { available = false, message = "National ID is required." });
            }

            var exists = await _context.Patients.AnyAsync(p => p.NationalId == nationalId);
            if (exists)
            {
                return Json(new { available = false, message = "This National ID is already registered." });
            }

            return Json(new { available = true });
        }

        // GET: /Account/CheckPhoneNumber
        [HttpGet]
        public async Task<IActionResult> CheckPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return Json(new { available = true });
            }

            var exists = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
            if (exists)
            {
                return Json(new { available = false, message = "This phone number is already registered." });
            }

            return Json(new { available = true });
        }
    }
}
