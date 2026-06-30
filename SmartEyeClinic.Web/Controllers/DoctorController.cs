using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
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

        // List Doctors (Accessible to all authenticated users)
        public IActionResult Index()
        {
            var doctors = _doctorService.GetAllDoctors();
            return View(doctors);
        }

        // Doctor 360 profile Details
        [HttpGet]
        public IActionResult Details(int id)
        {
            var doctor = _doctorService.GetDoctorById(id);
            if (doctor == null)
            {
                TempData["Error"] = "Doctor profile not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(doctor);
        }

        // Create Doctor (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Specializations = _context.Specializations.ToList();
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            string fullName,
            string email,
            string password,
            string phoneNumber,
            int specializationId, 
            string licenseNumber, 
            decimal consultationFee, 
            string? bio)
        {
            try
            {
                _doctorService.AddDoctor(fullName, email, password, phoneNumber, specializationId, licenseNumber, consultationFee, bio);
                TempData["Success"] = "Doctor registered successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.Specializations = _context.Specializations.ToList();
                return View();
            }
        }

        // Edit Doctor (Admin or the Doctor themselves)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var doctor = _doctorService.GetDoctorById(id);
            if (doctor == null)
                return NotFound();

            // Security check: Only Admins or the Doctor themselves can edit
            if (!User.IsInRole("Admin"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != id)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            ViewBag.Specializations = _context.Specializations.ToList();
            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
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
            // Security check: Only Admins or the Doctor themselves can edit
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
                _doctorService.UpdateDoctor(id, fullName, email, password, phoneNumber, specializationId, licenseNumber, consultationFee, bio);
                TempData["Success"] = "Doctor updated successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                ViewBag.Specializations = _context.Specializations.ToList();
                var doctor = _doctorService.GetDoctorById(id);
                return View(doctor);
            }
        }

        // Delete Doctor (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var doctor = _doctorService.GetDoctorById(id);
            if (doctor == null)
            {
                TempData["Error"] = "Doctor not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(doctor);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                _doctorService.DeleteDoctor(id);
                TempData["Success"] = "Doctor profile deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
