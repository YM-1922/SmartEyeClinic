using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Controllers
{
    [Authorize]
    public class SurgeryController : Controller
    {
        private readonly AppDbContext _context;

        public SurgeryController(AppDbContext context)
        {
            _context = context;
        }

        // List Surgeries — patients see only their own, doctors see only their own
        public async Task<IActionResult> Index()
        {
            var query = _context.Surgeries
                .Include(s => s.Patient).ThenInclude(p => p.User)
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.SurgeryType)
                .AsNoTracking()
                .AsQueryable();

            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null) return RedirectToAction("AccessDenied", "Account");
                int patId = int.Parse(patIdClaim);
                query = query.Where(s => s.PatientId == patId);
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null) return RedirectToAction("AccessDenied", "Account");
                int docId = int.Parse(docIdClaim);
                query = query.Where(s => s.DoctorId == docId);
            }

            var surgeries = await query.OrderByDescending(s => s.SurgeryDate).ToListAsync();
            return View(surgeries);
        }

        // Details — IDOR protected
        public async Task<IActionResult> Details(int id)
        {
            var surgery = await _context.Surgeries
                .Include(s => s.Patient).ThenInclude(p => p.User)
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.SurgeryType)
                .Include(s => s.Appointment)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (surgery == null)
                return NotFound();

            // IDOR Protection
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != surgery.PatientId)
                    return RedirectToAction("AccessDenied", "Account");
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != surgery.DoctorId)
                    return RedirectToAction("AccessDenied", "Account");
            }

            return View(surgery);
        }

        // Create (Doctors & Admin only)
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Create(int? appointmentId = null)
        {
            await PopulateDropdownsAsync(appointmentId);
            return View();
        }

        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Surgery surgery)
        {
            if (ModelState.IsValid)
            {
                _context.Surgeries.Add(surgery);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Surgery scheduled successfully!";
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropdownsAsync(surgery.AppointmentId);
            return View(surgery);
        }

        // Edit (Doctors & Admin only) — doctor can only edit their own
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var surgery = await _context.Surgeries.FindAsync(id);
            if (surgery == null)
                return NotFound();

            // IDOR: Doctor can only edit their own surgeries
            if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != surgery.DoctorId)
                    return RedirectToAction("AccessDenied", "Account");
            }

            await PopulateDropdownsAsync(surgery.AppointmentId);
            return View(surgery);
        }

        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Surgery surgery)
        {
            // IDOR: Doctor can only edit their own surgeries
            if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != surgery.DoctorId)
                    return RedirectToAction("AccessDenied", "Account");
            }

            if (ModelState.IsValid)
            {
                _context.Update(surgery);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Surgery record updated successfully!";
                return RedirectToAction(nameof(Details), new { id = surgery.Id });
            }
            await PopulateDropdownsAsync(surgery.AppointmentId);
            return View(surgery);
        }

        // Delete (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var surgery = await _context.Surgeries
                .Include(s => s.Patient).ThenInclude(p => p.User)
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.SurgeryType)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (surgery == null)
                return NotFound();

            return View(surgery);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var surgery = await _context.Surgeries.FindAsync(id);
            if (surgery == null)
                return NotFound();

            _context.Surgeries.Remove(surgery);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Surgery schedule deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? selectedAppId = null)
        {
            ViewBag.Appointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Where(a => a.Status != "Cancelled" || a.Id == selectedAppId)
                .AsNoTracking()
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            ViewBag.Patients = await _context.Patients
                .Include(p => p.User)
                .AsNoTracking()
                .OrderBy(p => p.User.FullName)
                .ToListAsync();

            ViewBag.Doctors = await _context.Doctors
                .Include(d => d.User)
                .AsNoTracking()
                .OrderBy(d => d.User.FullName)
                .ToListAsync();

            ViewBag.SurgeryTypes = await _context.SurgeryTypes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.SelectedAppointmentId = selectedAppId;
        }
    }
}
