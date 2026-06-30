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
    public class InvoiceController : Controller
    {
        private readonly InvoiceService _invoiceService;
        private readonly AppDbContext _context;

        public InvoiceController(InvoiceService invoiceService, AppDbContext context)
        {
            _invoiceService = invoiceService;
            _context = context;
        }

        // List Invoices
        public IActionResult Index()
        {
            var invoices = _invoiceService.GetAllInvoices();
            return View(invoices);
        }

        // Invoice details (Printable page)
        [HttpGet]
        public IActionResult Details(int id)
        {
            var invoice = _invoiceService.GetInvoiceById(id);
            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(invoice);
        }

        // Create Invoice (Receptionist or Admin only)
        [Authorize(Roles = "Admin,Receptionist")]
        [HttpGet]
        public IActionResult Create(int? appointmentId = null)
        {
            PopulateDropdowns(appointmentId);
            return View();
        }

        [Authorize(Roles = "Admin,Receptionist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int appointmentId, int patientId, decimal totalAmount, decimal? tax, decimal? discount)
        {
            var result = _invoiceService.AddInvoice(appointmentId, patientId, totalAmount, tax, discount);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                PopulateDropdowns(appointmentId);
                return View();
            }
            TempData["Success"] = "Invoice generated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Edit Invoice
        [Authorize(Roles = "Admin,Receptionist")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var invoice = _invoiceService.GetInvoiceById(id);
            if (invoice == null)
                return NotFound();

            PopulateDropdowns(invoice.AppointmentId);
            return View(invoice);
        }

        [Authorize(Roles = "Admin,Receptionist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
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
            var result = _invoiceService.UpdateInvoice(id, appointmentId, patientId, invoiceNumber, totalAmount, paidAmount, tax, discount, status);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                PopulateDropdowns(appointmentId);
                var invoice = _invoiceService.GetInvoiceById(id);
                return View(invoice);
            }
            TempData["Success"] = "Invoice updated successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Delete Invoice - GET
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var invoice = _invoiceService.GetInvoiceById(id);
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
        public IActionResult DeleteConfirmed(int id)
        {
            var result = _invoiceService.DeleteInvoice(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Invoice deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(int? selectedAppointmentId = null)
        {
            ViewBag.Appointments = _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Where(a => a.Status != "Cancelled" || a.Id == selectedAppointmentId)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToList();

            ViewBag.Patients = _context.Patients
                .Include(p => p.User)
                .OrderBy(p => p.User.FullName)
                .ToList();

            ViewBag.SelectedAppointmentId = selectedAppointmentId;
        }
    }
}
