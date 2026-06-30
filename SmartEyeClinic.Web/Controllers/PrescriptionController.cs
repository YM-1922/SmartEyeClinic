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
    public class PrescriptionController : Controller
    {
        private readonly PrescriptionService _prescriptionService;
        private readonly AppDbContext _context;

        public PrescriptionController(PrescriptionService prescriptionService, AppDbContext context)
        {
            _prescriptionService = prescriptionService;
            _context = context;
        }

        // List prescriptions (returns list of PrescriptionItem to match existing view contract)
        public IActionResult Index()
        {
            var prescriptions = _prescriptionService.GetAllPrescriptions();
            return View(prescriptions);
        }

        // Prescription details (Printable Rx sheet)
        [HttpGet]
        public IActionResult Details(int id)
        {
            var rxHeader = _prescriptionService.GetPrescriptionHeaderById(id);
            if (rxHeader == null)
            {
                TempData["Error"] = "Prescription not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(rxHeader);
        }

        // Create Prescription (Doctors only)
        [Authorize(Roles = "Doctor,Admin")]
        [HttpGet]
        public IActionResult Create(int? examinationId = null)
        {
            PopulateDropdowns(examinationId);
            return View();
        }

        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int examinationId, int medicineId, string? dosage, int durationDays, string? instructions)
        {
            var result = _prescriptionService.CreatePrescription(examinationId, medicineId, dosage, durationDays, instructions);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                PopulateDropdowns(examinationId);
                return View();
            }
            TempData["Success"] = "Prescription created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Delete Prescription - GET (Admin or prescribing Doctor)
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var rx = _prescriptionService.GetPrescriptionHeaderById(id);
            if (rx == null)
            {
                TempData["Error"] = "Prescription not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(rx);
        }

        // Delete Prescription - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var result = _prescriptionService.DeletePrescription(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Prescription deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(int? selectedExamId = null)
        {
            ViewBag.Examinations = _context.Examinations
                .Include(e => e.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .OrderByDescending(e => e.ExaminedAt)
                .ToList();

            ViewBag.Medicines = _context.Medicines
                .OrderBy(m => m.Name)
                .ToList();

            ViewBag.SelectedExaminationId = selectedExamId;
        }
    }
}
