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

        // إضافة طريقة دفع جديدة في النظام (مثل: بطاقة ائتمان، كاش، إلخ) مع التحقق من عدم التكرار
        public async Task<ServiceResult> AddPaymentMethodAsync(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult.Fail("Payment Method name cannot be empty!");

            bool exists = await _context.PaymentMethods.AnyAsync(m => m.Name.ToLower() == name.Trim().ToLower());
            if (exists)
                return ServiceResult.Fail("This payment method already exists.");

            var paymentMethod = new PaymentMethod
            {
                Name = name.Trim()
            };

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        // جلب قائمة بكافة طرق الدفع المتاحة والمسجلة في النظام
        public async Task<List<PaymentMethod>> GetAllPaymentMethodsAsync()
        {
            return await _context.PaymentMethods.AsNoTracking().ToListAsync();
        }

        // تسجيل عملية دفع جديدة وتحديث القيمة المدفوعة وحالة الفاتورة المرتبطة بها تلقائياً
        public async Task<ServiceResult> AddPaymentAsync(int invoiceId, int paymentMethodId,
            decimal amount, string? transactionRef)
        {
            var invoice      = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
            var methodExists = await _context.PaymentMethods.AnyAsync(m => m.Id == paymentMethodId);

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

            // تحديث إجمالي المبالغ المدفوعة على الفاتورة وتغيير حالتها (مدفوعة كلياً أو جزئياً)
            invoice.PaidAmount = (invoice.PaidAmount ?? 0) + amount;
            if (invoice.PaidAmount >= invoice.TotalAmount)
                invoice.Status = "Paid";
            else
                invoice.Status = "Partial";

            await _context.SaveChangesAsync();

            return ServiceResult.Ok();
        }

        // جلب قائمة بكافة عمليات الدفع المسجلة بالنظام مع تفاصيل الفواتير والمرضى
        public async Task<List<Payment>> GetAllPaymentsAsync()
        {
            return await _context.Payments
                .Include(p => p.Invoice).ThenInclude(i => i.Patient).ThenInclude(pa => pa.User)
                .Include(p => p.PaymentMethod)
                .AsNoTracking()
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();
        }

        // جلب تفاصيل عملية دفع محددة باستخدام المعرف الفريد الخاص بها
        public async Task<Payment?> GetPaymentByIdAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.Invoice).ThenInclude(i => i.Patient).ThenInclude(pa => pa.User)
                .Include(p => p.PaymentMethod)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // حذف عملية دفع وتعديل المبالغ المدفوعة في الفاتورة المرتبطة بها (عكس وإرجاع عملية الدفع)
        public async Task<ServiceResult> DeletePaymentAsync(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return ServiceResult.Fail("Payment record not found.");

            var invoice = payment.Invoice;
            if (invoice != null)
            {
                // إرجاع قيمة الدفعة وتعديل حالة الفاتورة تبعا للمبالغ المتبقية بعد الحذف
                invoice.PaidAmount = Math.Max(0, (invoice.PaidAmount ?? 0) - payment.Amount);
                if (invoice.PaidAmount == 0)
                    invoice.Status = "Unpaid";
                else if (invoice.PaidAmount < invoice.TotalAmount)
                    invoice.Status = "Partial";
                else
                    invoice.Status = "Paid";
            }

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok();
        }
    }
}
