using System;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Services
{
    public class PaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        public void AddPaymentMethod()
        {
            Console.Write("Payment Method Name (e.g. Cash, Visa, MasterCard): ");
            string? name = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Payment Method name cannot be empty!");
                return;
            }

            var paymentMethod = new PaymentMethod
            {
                Name = name.Trim()
            };

            _context.PaymentMethods.Add(paymentMethod);
            _context.SaveChanges();

            Console.WriteLine("Payment Method Added Successfully!");
        }

        public void ShowPaymentMethods()
        {
            var methods = _context.PaymentMethods.ToList();

            Console.WriteLine("\n===== Payment Methods =====");

            foreach (var m in methods)
            {
                Console.WriteLine($"ID: {m.Id} | Name: {m.Name}");
            }
        }

        public void AddPayment()
        {
            Console.Write("Invoice Id: ");
            int invoiceId = int.Parse(Console.ReadLine()!);

            Console.Write("Payment Method Id: ");
            int paymentMethodId = int.Parse(Console.ReadLine()!);

            var invoice = _context.Invoices.FirstOrDefault(i => i.Id == invoiceId);
            var methodExists = _context.PaymentMethods.Any(m => m.Id == paymentMethodId);

            if (invoice == null)
            {
                Console.WriteLine("Invoice not found!");
                return;
            }

            if (!methodExists)
            {
                Console.WriteLine("Payment Method not found!");
                return;
            }

            Console.Write("Amount: ");
            decimal amount = decimal.Parse(Console.ReadLine()!);

            Console.Write("Transaction Reference (or leave blank): ");
            string? transactionRef = Console.ReadLine();

            var payment = new Payment
            {
                InvoiceId = invoiceId,
                PaymentMethodId = paymentMethodId,
                Amount = amount,
                TransactionRef = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef,
                PaidAt = DateTime.Now
            };

            _context.Payments.Add(payment);

            // Update invoice paid amount and status
            invoice.PaidAmount = (invoice.PaidAmount ?? 0) + amount;
            if (invoice.PaidAmount >= invoice.TotalAmount)
            {
                invoice.Status = "Paid";
            }
            else
            {
                invoice.Status = "Partial";
            }

            _context.SaveChanges();

            Console.WriteLine($"Payment Added Successfully! Invoice Status: {invoice.Status}");
        }

        public void ShowPayments()
        {
            var payments = _context.Payments
                .Include(p => p.Invoice)
                .ThenInclude(i => i.Patient)
                .ThenInclude(pa => pa.User)
                .Include(p => p.PaymentMethod)
                .ToList();

            Console.WriteLine("\n===== Payments =====");

            foreach (var p in payments)
            {
                Console.WriteLine(
                    $"ID: {p.Id} | " +
                    $"Invoice#: {p.Invoice.InvoiceNumber} | " +
                    $"Patient: {p.Invoice.Patient.User.FullName} | " +
                    $"Method: {p.PaymentMethod.Name} | " +
                    $"Amount: {p.Amount:C} | " +
                    $"PaidAt: {p.PaidAt}"
                );
            }
        }
    }
}
