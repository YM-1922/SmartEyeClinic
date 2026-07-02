using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Controllers
{
    [Authorize]
    public class MedicalFileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        // Allowed file extensions and their MIME types
        private static readonly Dictionary<string, string[]> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf",  new[] { "application/pdf" } },
            { ".jpg",  new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".png",  new[] { "image/png" } },
            { ".doc",  new[] { "application/msword" } },
            { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
        };

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public MedicalFileController(AppDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // List files (Admin / Doctor can see all, Patients see only their own)
        public async Task<IActionResult> Index(int? patientId = null)
        {
            IQueryable<MedicalFile> query = _context.MedicalFiles
                .Include(f => f.Patient).ThenInclude(p => p.User)
                .Include(f => f.Uploader)
                .Include(f => f.Appointment);

            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (string.IsNullOrEmpty(patIdClaim))
                    return RedirectToAction("AccessDenied", "Account");
                
                int patId = int.Parse(patIdClaim);
                query = query.Where(f => f.PatientId == patId);
                ViewBag.IsPatient = true;
                ViewBag.PatientId = patId;
            }
            else
            {
                if (patientId.HasValue)
                {
                    query = query.Where(f => f.PatientId == patientId.Value);
                    ViewBag.PatientId = patientId.Value;
                }
                ViewBag.IsPatient = false;
            }

            var files = await query.AsNoTracking().OrderByDescending(f => f.UploadedAt).ToListAsync();
            return View(files);
        }

        // GET: Upload File — restricted to Admin, Doctor, Receptionist
        [Authorize(Roles = "Admin,Doctor,Receptionist")]
        [HttpGet]
        public async Task<IActionResult> Upload(int? patientId = null, int? appointmentId = null)
        {
            ViewBag.PatientId = patientId;
            ViewBag.AppointmentId = appointmentId;
            
            ViewBag.Patients = await _context.Patients.Include(p => p.User)
                .AsNoTracking().OrderBy(p => p.User.FullName).ToListAsync();
            ViewBag.Appointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.Status != "Cancelled")
                .AsNoTracking()
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            return View();
        }

        // POST: Upload File — restricted to Admin, Doctor, Receptionist
        [Authorize(Roles = "Admin,Doctor,Receptionist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int patientId, int? appointmentId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Upload), new { patientId, appointmentId });
            }

            // --- Security: Size check ---
            if (file.Length > MaxFileSizeBytes)
            {
                TempData["Error"] = "File size exceeds the maximum allowed limit of 5 MB.";
                return RedirectToAction(nameof(Upload), new { patientId, appointmentId });
            }

            // --- Security: Extension whitelist ---
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedTypes.ContainsKey(ext))
            {
                TempData["Error"] = "Invalid file type. Allowed: PDF, JPG, JPEG, PNG, DOC, DOCX.";
                return RedirectToAction(nameof(Upload), new { patientId, appointmentId });
            }

            // --- Security: MIME type validation ---
            var allowedMimes = AllowedTypes[ext];
            if (!allowedMimes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] = "File content type does not match the file extension. Upload rejected.";
                return RedirectToAction(nameof(Upload), new { patientId, appointmentId });
            }

            try
            {
                // Ensure upload folder exists
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                // Generate unique filename to avoid duplicates / path traversal
                var safeFilename = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadDir, safeFilename);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Get current logged-in user ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int? uploaderId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;

                // Save file info in Database
                var medicalFile = new MedicalFile
                {
                    PatientId     = patientId,
                    AppointmentId = appointmentId,
                    UploadedBy    = uploaderId,
                    FileType      = ext.TrimStart('.').ToUpper(),
                    FilePath      = $"/uploads/{safeFilename}",
                    FileSize      = file.Length,
                    UploadedAt    = DateTime.Now
                };

                _context.MedicalFiles.Add(medicalFile);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Medical file uploaded successfully!";
                return RedirectToAction(nameof(Index), new { patientId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"File upload failed: {ex.Message}";
                return RedirectToAction(nameof(Upload), new { patientId, appointmentId });
            }
        }

        // Delete File — GET (Admin or uploader)
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var file = await _context.MedicalFiles
                .Include(f => f.Patient).ThenInclude(p => p.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
                return NotFound();

            // Patients cannot delete; Admin and original uploader (Doctor/Receptionist) can
            if (User.IsInRole("Patient"))
                return RedirectToAction("AccessDenied", "Account");

            // Non-admin users can only delete files they uploaded
            if (!User.IsInRole("Admin"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || file.UploadedBy != int.Parse(userIdClaim.Value))
                    return RedirectToAction("AccessDenied", "Account");
            }

            return View(file);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var file = await _context.MedicalFiles.FindAsync(id);
            if (file == null)
                return NotFound();

            // Re-check authorization on POST as well (defense in depth)
            if (User.IsInRole("Patient"))
                return RedirectToAction("AccessDenied", "Account");

            if (!User.IsInRole("Admin"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || file.UploadedBy != int.Parse(userIdClaim.Value))
                    return RedirectToAction("AccessDenied", "Account");
            }

            // Remove file from disk
            var diskPath = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(diskPath))
                System.IO.File.Delete(diskPath);

            int pid = file.PatientId;
            _context.MedicalFiles.Remove(file);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Medical file deleted successfully.";
            return RedirectToAction(nameof(Index), new { patientId = pid });
        }
    }
}
