using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services
{
    public class PaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        // Add Payment Method
        public ServiceResult AddPaymentMethod(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult.Fail("Payment Method name cannot be empty!");

            bool exists = _context.PaymentMethods.Any(m => m.Name.ToLower() == name.Trim().ToLower());
            if (exists)
                return ServiceResult.Fail("This payment method already exists.");

            var paymentMethod = new PaymentMethod
            {
                Name = name.Trim()
            };

            _context.PaymentMethods.Add(paymentMethod);
            _context.SaveChanges();

            return ServiceResult.Ok();
        }

        // Get All Payment Methods
        public List<PaymentMethod> GetAllPaymentMethods()
        {
            return _context.PaymentMethods.ToList();
        }

        // Add Payment
        public ServiceResult AddPayment(int invoiceId, int paymentMethodId,
            decimal amount, string? transactionRef)
        {
            var invoice      = _context.Invoices.FirstOrDefault(i => i.Id == invoiceId);
            var methodExists = _context.PaymentMethods.Any(m => m.Id == paymentMethodId);

            if (invoice == null)
                return ServiceResult.Fail("Invoice not found!");

            if (!methodExists)
                return ServiceResult.Fail("Payment Method not found!");

            if (amount <= 0)
                return ServiceResult.Fail("Payment amount must be greater than zero.");

            var payment = new Payment
            {
                InvoiceId       = invoiceId,
                PaymentMethodId = paymentMethodId,
                Amount          = amount,
                TransactionRef  = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef,
                PaidAt          = DateTime.Now
            };

            _context.Payments.Add(payment);

            // Update invoice paid amount and status
            invoice.PaidAmount = (invoice.PaidAmount ?? 0) + amount;
            if (invoice.PaidAmount >= invoice.TotalAmount)
                invoice.Status = "Paid";
            else
                invoice.Status = "Partial";

            _context.SaveChanges();

            return ServiceResult.Ok();
        }

        // Get All Payments
        public List<Payment> GetAllPayments()
        {
            return _context.Payments
                .Include(p => p.Invoice).ThenInclude(i => i.Patient).ThenInclude(pa => pa.User)
                .Include(p => p.PaymentMethod)
                .OrderByDescending(p => p.PaidAt)
                .ToList();
        }

        // Get Payment By Id
        public Payment? GetPaymentById(int id)
        {
            return _context.Payments
                .Include(p => p.Invoice).ThenInclude(i => i.Patient).ThenInclude(pa => pa.User)
                .Include(p => p.PaymentMethod)
                .FirstOrDefault(p => p.Id == id);
        }

        // Delete Payment (with Invoice refund/reversal logic)
        public ServiceResult DeletePayment(int id)
        {
            var payment = _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefault(p => p.Id == id);

            if (payment == null)
                return ServiceResult.Fail("Payment record not found.");

            var invoice = payment.Invoice;
            if (invoice != null)
            {
                // Revert paid amount
                invoice.PaidAmount = Math.Max(0, (invoice.PaidAmount ?? 0) - payment.Amount);
                if (invoice.PaidAmount == 0)
                    invoice.Status = "Unpaid";
                else if (invoice.PaidAmount < invoice.TotalAmount)
                    invoice.Status = "Partial";
                else
                    invoice.Status = "Paid";
            }

            _context.Payments.Remove(payment);
            _context.SaveChanges();

            return ServiceResult.Ok();
        }
    }
}
