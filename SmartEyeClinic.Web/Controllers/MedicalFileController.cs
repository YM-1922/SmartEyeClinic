using System;
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

            var files = await query.OrderByDescending(f => f.UploadedAt).ToListAsync();
            return View(files);
        }

        // GET: Upload File
        [HttpGet]
        public IActionResult Upload(int? patientId = null, int? appointmentId = null)
        {
            ViewBag.PatientId = patientId;
            ViewBag.AppointmentId = appointmentId;
            
            ViewBag.Patients = _context.Patients.Include(p => p.User).OrderBy(p => p.User.FullName).ToList();
            ViewBag.Appointments = _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.Status != "Cancelled")
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToList();

            return View();
        }

        // POST: Upload File
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int patientId, int? appointmentId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Upload), new { patientId, appointmentId });
            }

            try
            {
                // Ensure upload folder exists
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                // Generate unique filename to avoid duplicates
                var ext = Path.GetExtension(file.FileName);
                var filename = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadDir, filename);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Get current logged in user ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int? uploaderId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;

                // Save file info in Database
                var medicalFile = new MedicalFile
                {
                    PatientId = patientId,
                    AppointmentId = appointmentId,
                    UploadedBy = uploaderId,
                    FileType = ext.TrimStart('.').ToUpper(),
                    FilePath = $"/uploads/{filename}",
                    FileSize = file.Length,
                    UploadedAt = DateTime.Now
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

        // Delete File
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var file = await _context.MedicalFiles
                .Include(f => f.Patient).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
                return NotFound();

            // Constraint check: Patients cannot delete files unless they uploaded them, or Admin only
            if (User.IsInRole("Patient"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || file.UploadedBy != int.Parse(userIdClaim.Value))
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
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

            // Remove file from disk
            var diskPath = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(diskPath))
            {
                System.IO.File.Delete(diskPath);
            }

            int pid = file.PatientId;
            _context.MedicalFiles.Remove(file);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Medical file deleted successfully.";
            return RedirectToAction(nameof(Index), new { patientId = pid });
        }
    }
}
