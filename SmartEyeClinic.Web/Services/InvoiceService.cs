using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services
{
    public class InvoiceService
    {
        private readonly AppDbContext _context;

        public InvoiceService(AppDbContext context)
        {
            _context = context;
        }

        // Get All Invoices
        public List<Invoice> GetAllInvoices()
        {
            return _context.Invoices
                .Include(i => i.Patient).ThenInclude(p => p.User)
                .Include(i => i.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .OrderByDescending(i => i.IssuedAt)
                .ToList();
        }

        // Get Invoice By Id
        public Invoice? GetInvoiceById(int id)
        {
            return _context.Invoices
                .Include(i => i.Patient).ThenInclude(p => p.User)
                .Include(i => i.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .Include(i => i.Appointment).ThenInclude(a => a.Branch)
                .Include(i => i.Payments).ThenInclude(p => p.PaymentMethod)
                .FirstOrDefault(i => i.Id == id);
        }

        // Add Invoice
        public ServiceResult AddInvoice(int appointmentId, int patientId,
            decimal totalAmount, decimal? tax, decimal? discount)
        {
            var appointmentExists = _context.Appointments.Any(a => a.Id == appointmentId);
            var patientExists     = _context.Patients.Any(p => p.Id == patientId);

            if (!appointmentExists)
                return ServiceResult.Fail("Appointment not found!");

            if (!patientExists)
                return ServiceResult.Fail("Patient not found!");

            var invoice = new Invoice
            {
                AppointmentId = appointmentId,
                PatientId     = patientId,
                InvoiceNumber = $"INV-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                TotalAmount   = totalAmount,
                PaidAmount    = 0,
                Tax           = tax,
                Discount      = discount,
                Status        = "Unpaid",
                IssuedAt      = DateTime.Now
            };

            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            return ServiceResult.Ok();
        }

        // Update Invoice
        public ServiceResult UpdateInvoice(
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
            var invoice = _context.Invoices.Find(id);
            if (invoice == null)
                return ServiceResult.Fail("Invoice not found.");

            if (!_context.Appointments.Any(a => a.Id == appointmentId))
                return ServiceResult.Fail("Appointment not found!");

            if (!_context.Patients.Any(p => p.Id == patientId))
                return ServiceResult.Fail("Patient not found!");

            invoice.AppointmentId = appointmentId;
            invoice.PatientId = patientId;
            invoice.InvoiceNumber = invoiceNumber;
            invoice.TotalAmount = totalAmount;
            invoice.PaidAmount = paidAmount ?? 0;
            invoice.Tax = tax;
            invoice.Discount = discount;
            invoice.Status = status;

            _context.SaveChanges();
            return ServiceResult.Ok();
        }

        // Delete Invoice
        public ServiceResult DeleteInvoice(int id)
        {
            var invoice = _context.Invoices.Find(id);
            if (invoice == null)
                return ServiceResult.Fail("Invoice not found.");

            // Check if invoice has associated payment records
            if (_context.Payments.Any(p => p.InvoiceId == id))
                return ServiceResult.Fail("Cannot delete invoice because payments have already been settled against it.");

            _context.Invoices.Remove(invoice);
            _context.SaveChanges();
            return ServiceResult.Ok();
        }
    }
}
