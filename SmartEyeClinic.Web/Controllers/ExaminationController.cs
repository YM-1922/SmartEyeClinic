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
    public class ExaminationController : Controller
    {
        private readonly ExaminationService _examinationService;
        private readonly AppDbContext _context;

        public ExaminationController(ExaminationService examinationService, AppDbContext context)
        {
            _examinationService = examinationService;
            _context = context;
        }

        // List Examinations
        public IActionResult Index()
        {
            var examinations = _examinationService.GetAllExaminations();
            return View(examinations);
        }

        // Examination Details (Visual Acuity sheet)
        [HttpGet]
        public IActionResult Details(int id)
        {
            var exam = _examinationService.GetExaminationById(id);
            if (exam == null)
            {
                TempData["Error"] = "Examination record not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        // Create Examination (Doctors only)
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public IActionResult Create(int? appointmentId = null)
        {
            PopulateAppointmentsDropdown(appointmentId);
            return View();
        }

        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            int appointmentId, 
            string diagnosis, 
            string? symptoms,
            string? visualAcuityLeft, 
            string? visualAcuityRight, 
            string? intraocularPressure, 
            string? treatmentPlan)
        {
            var result = _examinationService.AddExamination(appointmentId, diagnosis, symptoms,
                visualAcuityLeft, visualAcuityRight, intraocularPressure, treatmentPlan);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                PopulateAppointmentsDropdown(appointmentId);
                return View();
            }
            TempData["Success"] = "Examination record logged successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Edit Examination
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var exam = _examinationService.GetExaminationById(id);
            if (exam == null)
                return NotFound();

            PopulateAppointmentsDropdown(exam.AppointmentId);
            return View(exam);
        }

        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            int id,
            int appointmentId,
            string diagnosis,
            string? symptoms,
            string? visualAcuityLeft,
            string? visualAcuityRight,
            string? intraocularPressure,
            string? treatmentPlan)
        {
            var result = _examinationService.UpdateExamination(id, appointmentId, diagnosis, symptoms,
                visualAcuityLeft, visualAcuityRight, intraocularPressure, treatmentPlan);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                PopulateAppointmentsDropdown(appointmentId);
                var exam = _examinationService.GetExaminationById(id);
                return View(exam);
            }
            TempData["Success"] = "Examination record updated successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Delete Examination - GET
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var exam = _examinationService.GetExaminationById(id);
            if (exam == null)
            {
                TempData["Error"] = "Examination record not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        // Delete Examination - POST
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var result = _examinationService.DeleteExamination(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Examination record deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateAppointmentsDropdown(int? selectedId = null)
        {
            // Populate appointments that are pending or matching selectedId
            ViewBag.Appointments = _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Where(a => a.Status != "Cancelled" || a.Id == selectedId)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToList();
            ViewBag.SelectedAppointmentId = selectedId;
        }
    }
}
