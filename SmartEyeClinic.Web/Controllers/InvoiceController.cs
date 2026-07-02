using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;
using SmartEyeClinic.Web.Services;

namespace SmartEyeClinic.Web.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly InvoiceService _invoiceService;
        private readonly AppDbContext _context;

        public InvoiceController(InvoiceService invoiceService, AppDbContext context)
        {
            _invoiceService = invoiceService;
            _context = context;
        }

        // List Invoices (patients see only their own)
        public async Task<IActionResult> Index()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();

            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim != null)
                {
                    int patId = int.Parse(patIdClaim);
                    invoices = invoices.Where(i => i.PatientId == patId).ToList();
                }
                else
                {
                    invoices = new List<Invoice>();
                }
            }

            return View(invoices);
        }

        // Invoice details — IDOR protected
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction(nameof(Index));
            }

            // IDOR: patients may only view their own invoice
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != invoice.PatientId)
                    return RedirectToAction("AccessDenied", "Account");
            }

            return View(invoice);
        }

        // Create Invoice (Receptionist or Admin only)
        [Authorize(Roles = "Admin,Receptionist")]
        [HttpGet]
        public async Task<IActionResult> Create(int? appointmentId = null)
        {
            await PopulateDropdownsAsync(appointmentId);
            return View();
        }

        [Authorize(Roles = "Admin,Receptionist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int appointmentId, int patientId, decimal totalAmount, decimal? tax, decimal? discount)
        {
            var result = await _invoiceService.AddInvoiceAsync(appointmentId, patientId, totalAmount, tax, discount);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await PopulateDropdownsAsync(appointmentId);
                return View();
            }
            TempData["Success"] = "Invoice generated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Edit Invoice
        [Authorize(Roles = "Admin,Receptionist")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
                return NotFound();

            await PopulateDropdownsAsync(invoice.AppointmentId);
            return View(invoice);
        }

        [Authorize(Roles = "Admin,Receptionist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int appointmentId,
            int patientId,
            string invoiceNumber,
            decimal totalAmount,
            decimal? paidAmount,
            decimal? tax,
            decimal? discount,
            string status)
        {
            var result = await _invoiceService.UpdateInvoiceAsync(id, appointmentId, patientId, invoiceNumber, totalAmount, paidAmount, tax, discount, status);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await PopulateDropdownsAsync(appointmentId);
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                return View(invoice);
            }
            TempData["Success"] = "Invoice updated successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Delete Invoice - GET
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(invoice);
        }

        // Delete Invoice - POST
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _invoiceService.DeleteInvoiceAsync(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Invoice deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? selectedAppointmentId = null)
        {
            ViewBag.Appointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Where(a => a.Status != "Cancelled" || a.Id == selectedAppointmentId)
                .AsNoTracking()
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            ViewBag.Patients = await _context.Patients
                .Include(p => p.User)
                .AsNoTracking()
                .OrderBy(p => p.User.FullName)
                .ToListAsync();

            ViewBag.SelectedAppointmentId = selectedAppointmentId;
        }
    }
}
