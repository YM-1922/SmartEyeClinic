using System;
using System.Linq;
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

        // List Surgeries
        public IActionResult Index()
        {
            var surgeries = _context.Surgeries
                .Include(s => s.Patient).ThenInclude(p => p.User)
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.SurgeryType)
                .OrderByDescending(s => s.SurgeryDate)
                .ToList();
            return View(surgeries);
        }

        // Details
        public IActionResult Details(int id)
        {
            var surgery = _context.Surgeries
                .Include(s => s.Patient).ThenInclude(p => p.User)
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.SurgeryType)
                .Include(s => s.Appointment)
                .FirstOrDefault(s => s.Id == id);

            if (surgery == null)
                return NotFound();

            return View(surgery);
        }

        // Create (Doctors & Admin only)
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public IActionResult Create(int? appointmentId = null)
        {
            PopulateDropdowns(appointmentId);
            return View();
        }

        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Surgery surgery)
        {
            if (ModelState.IsValid)
            {
                _context.Surgeries.Add(surgery);
                _context.SaveChanges();
                TempData["Success"] = "Surgery scheduled successfully!";
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(surgery.AppointmentId);
            return View(surgery);
        }

        // Edit (Doctors & Admin only)
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var surgery = _context.Surgeries.Find(id);
            if (surgery == null)
                return NotFound();

            PopulateDropdowns(surgery.AppointmentId);
            return View(surgery);
        }

        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Surgery surgery)
        {
            if (ModelState.IsValid)
            {
                _context.Update(surgery);
                _context.SaveChanges();
                TempData["Success"] = "Surgery record updated successfully!";
                return RedirectToAction(nameof(Details), new { id = surgery.Id });
            }
            PopulateDropdowns(surgery.AppointmentId);
            return View(surgery);
        }

        // Delete (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var surgery = _context.Surgeries
                .Include(s => s.Patient).ThenInclude(p => p.User)
                .Include(s => s.Doctor).ThenInclude(d => d.User)
                .Include(s => s.SurgeryType)
                .FirstOrDefault(s => s.Id == id);

            if (surgery == null)
                return NotFound();

            return View(surgery);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var surgery = _context.Surgeries.Find(id);
            if (surgery == null)
                return NotFound();

            _context.Surgeries.Remove(surgery);
            _context.SaveChanges();
            TempData["Success"] = "Surgery schedule deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(int? selectedAppId = null)
        {
            ViewBag.Appointments = _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Where(a => a.Status != "Cancelled" || a.Id == selectedAppId)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToList();

            ViewBag.Patients = _context.Patients
                .Include(p => p.User)
                .OrderBy(p => p.User.FullName)
                .ToList();

            ViewBag.Doctors = _context.Doctors
                .Include(d => d.User)
                .OrderBy(d => d.User.FullName)
                .ToList();

            ViewBag.SurgeryTypes = _context.SurgeryTypes
                .OrderBy(t => t.Name)
                .ToList();

            ViewBag.SelectedAppointmentId = selectedAppId;
        }
    }
}
