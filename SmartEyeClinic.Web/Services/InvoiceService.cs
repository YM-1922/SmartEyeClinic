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
        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Patient).ThenInclude(p => p.User)
                .Include(i => i.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .AsNoTracking()
                .OrderByDescending(i => i.IssuedAt)
                .ToListAsync();
        }

        // Get Invoice By Id
        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.Patient).ThenInclude(p => p.User)
                .Include(i => i.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .Include(i => i.Appointment).ThenInclude(a => a.Branch)
                .Include(i => i.Payments).ThenInclude(p => p.PaymentMethod)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        // Add Invoice
        public async Task<ServiceResult> AddInvoiceAsync(int appointmentId, int patientId,
            decimal totalAmount, decimal? tax, decimal? discount)
        {
            var appointmentExists = await _context.Appointments.AnyAsync(a => a.Id == appointmentId);
            var patientExists     = await _context.Patients.AnyAsync(p => p.Id == patientId);

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
            await _context.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        // Update Invoice
        public async Task<ServiceResult> UpdateInvoiceAsync(
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
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
                return ServiceResult.Fail("Invoice not found.");

            if (!await _context.Appointments.AnyAsync(a => a.Id == appointmentId))
                return ServiceResult.Fail("Appointment not found!");

            if (!await _context.Patients.AnyAsync(p => p.Id == patientId))
                return ServiceResult.Fail("Patient not found!");

            invoice.AppointmentId = appointmentId;
            invoice.PatientId = patientId;
            invoice.InvoiceNumber = invoiceNumber;
            invoice.TotalAmount = totalAmount;
            invoice.PaidAmount = paidAmount ?? 0;
            invoice.Tax = tax;
            invoice.Discount = discount;
            invoice.Status = status;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        // Delete Invoice
        public async Task<ServiceResult> DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
                return ServiceResult.Fail("Invoice not found.");

            // Check if invoice has associated payment records
            if (await _context.Payments.AnyAsync(p => p.InvoiceId == id))
                return ServiceResult.Fail("Cannot delete invoice because payments have already been settled against it.");

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok();
        }
    }
}
