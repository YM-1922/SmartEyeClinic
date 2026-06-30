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

        // GET: /Account/Login
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

        // POST: /Account/Login
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

            if (user == null || user.PasswordHash != password) // Plaintext comparison for simplicity & existing seed data
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View();
            }

            // Create claims list
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            // Retrieve profile ID depending on role to store as claims for fast lookup
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

            // Profile Picture claim
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

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password, string phoneNumber, string nationalId, string gender, string address, DateOnly dob)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(nationalId))
            {
                ModelState.AddModelError(string.Empty, "All fields marked with * are required.");
                return View();
            }

            // Check email uniqueness
            var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                ModelState.AddModelError("email", "This email address is already registered.");
                return View();
            }

            // Check phone uniqueness
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
                if (phoneExists)
                {
                    ModelState.AddModelError("phoneNumber", "This phone number is already registered.");
                    return View();
                }
            }

            // Check national ID uniqueness
            var nidExists = await _context.Patients.AnyAsync(p => p.NationalId == nationalId);
            if (nidExists)
            {
                ModelState.AddModelError("nationalId", "This National ID is already registered.");
                return View();
            }

            // Get Patient Role
            var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Patient");
            if (patientRole == null)
            {
                ModelState.AddModelError(string.Empty, "Patient role is not initialized in the database.");
                return View();
            }

            // Create User
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

            // Create Patient Profile
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

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Account/Profile
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

        // POST: /Account/EditProfile
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

            // Check phone uniqueness
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
            
            // Re-sign in to refresh cookie claims
            var identity = (ClaimsIdentity)User.Identity!;
            var nameClaim = identity.FindFirst(ClaimTypes.Name);
            if (nameClaim != null) identity.RemoveClaim(nameClaim);
            identity.AddClaim(new Claim(ClaimTypes.Name, fullName));

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Account/ChangePassword
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
    }
}
