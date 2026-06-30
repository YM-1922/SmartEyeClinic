using System;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Services
{
    public class InvoiceService
    {
        private readonly AppDbContext _context;

        public InvoiceService(AppDbContext context)
        {
            _context = context;
        }

        public void AddInvoice()
        {
            Console.Write("Appointment Id: ");
            int appointmentId = int.Parse(Console.ReadLine()!);

            Console.Write("Patient Id: ");
            int patientId = int.Parse(Console.ReadLine()!);

            var appointmentExists = _context.Appointments.Any(a => a.Id == appointmentId);
            var patientExists = _context.Patients.Any(p => p.Id == patientId);

            if (!appointmentExists)
            {
                Console.WriteLine("Appointment not found!");
                return;
            }

            if (!patientExists)
            {
                Console.WriteLine("Patient not found!");
                return;
            }

            Console.Write("Total Amount: ");
            decimal totalAmount = decimal.Parse(Console.ReadLine()!);

            Console.Write("Tax (or leave blank): ");
            string? taxInput = Console.ReadLine();
            decimal? tax = string.IsNullOrWhiteSpace(taxInput) ? null : decimal.Parse(taxInput);

            Console.Write("Discount (or leave blank): ");
            string? discountInput = Console.ReadLine();
            decimal? discount = string.IsNullOrWhiteSpace(discountInput) ? null : decimal.Parse(discountInput);

            var invoice = new Invoice
            {
                AppointmentId = appointmentId,
                PatientId = patientId,
                InvoiceNumber = $"INV-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                TotalAmount = totalAmount,
                PaidAmount = 0,
                Tax = tax,
                Discount = discount,
                Status = "Unpaid",
                IssuedAt = DateTime.Now
            };

            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            Console.WriteLine($"Invoice Added Successfully! Invoice Number: {invoice.InvoiceNumber}");
        }

        public void ShowInvoices()
        {
            var invoices = _context.Invoices
                .Include(i => i.Patient)
                .ThenInclude(p => p.User)
                .Include(i => i.Appointment)
                .ToList();

            Console.WriteLine("\n===== Invoices =====");

            foreach (var i in invoices)
            {
                Console.WriteLine(
                    $"ID: {i.Id} | " +
                    $"Invoice#: {i.InvoiceNumber} | " +
                    $"Patient: {i.Patient.User.FullName} | " +
                    $"Total: {i.TotalAmount:C} | " +
                    $"Paid: {i.PaidAmount:C} | " +
                    $"Status: {i.Status}"
                );
            }
        }
    }
}
