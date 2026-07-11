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
    public class PaymentController : Controller
    {
        private readonly PaymentService _paymentService;
        private readonly AppDbContext _context;

        public PaymentController(PaymentService paymentService, AppDbContext context)
        {
            _paymentService = paymentService;
            _context = context;
        }

        // GET: /Payment/Index | استعراض سجل المدفوعات وتصفيتها للمرضى لرؤية دفعاتهم الخاصة فقط
        public async Task<IActionResult> Index()
        {
            var payments = await _paymentService.GetAllPaymentsAsync();

            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim != null)
                {
                    int patId = int.Parse(patIdClaim);
                    payments = payments.Where(p => p.Invoice.PatientId == patId).ToList();
                }
                else
                {
                    payments = new List<Payment>();
                }
            }

            return View(payments);
        }

        // GET: /Payment/Details/{id} | تفاصيل عملية الدفع مع حماية الحسابات من التلاعب (IDOR Protection)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction(nameof(Index));
            }

            // التحقق من صلاحيات المريض لمنع استعراض عمليات دفع الآخرين
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != payment.Invoice.PatientId)
                    return RedirectToAction("AccessDenied", "Account");
            }

            return View(payment);
        }

        // GET: /Payment/Create | نموذج تسجيل عملية دفع جديدة لفاتورة معينة
        [Authorize(Roles = "Admin,Receptionist")]
        [HttpGet]
        public async Task<IActionResult> Create(int? invoiceId = null)
        {
            await PopulateDropdownsAsync(invoiceId);
            return View();
        }

        // POST: /Payment/Create | معالجة دفع الفاتورة وتسجيل مبلغ العملية وحفظ المرجع المالي
        [Authorize(Roles = "Admin,Receptionist")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int invoiceId, int paymentMethodId, decimal amount, string? transactionRef)
        {
            var result = await _paymentService.AddPaymentAsync(invoiceId, paymentMethodId, amount, transactionRef);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await PopulateDropdownsAsync(invoiceId);
                return View();
            }
            TempData["Success"] = "Payment registered successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Payment/Delete/{id} | صفحة تأكيد إلغاء/عكس عملية دفع معينة (المدير فقط)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(payment);
        }

        // POST: /Payment/Delete/{id} | عكس/إلغاء عملية الدفع نهائياً وتحديث حالة الفاتورة المرتبطة
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _paymentService.DeletePaymentAsync(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Payment reversed successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Payment/Methods | استعراض طرق الدفع المتاحة في النظام
        public async Task<IActionResult> Methods()
        {
            var methods = await _paymentService.GetAllPaymentMethodsAsync();
            return View(methods);
        }

        // GET: /Payment/AddMethod | نموذج إضافة طريقة دفع جديدة للادارة
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult AddMethod()
        {
            return View();
        }

        // POST: /Payment/AddMethod | معالجة إضافة وحفظ طريقة الدفع الجديدة
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMethod(string name)
        {
            var result = await _paymentService.AddPaymentMethodAsync(name);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View();
            }
            TempData["Success"] = "Payment method added successfully!";
            return RedirectToAction(nameof(Methods));
        }

        // دالة مساعدة لتعبئة قوائم الفواتير غير المدفوعة وطرق الدفع في النماذج
        private async Task PopulateDropdownsAsync(int? selectedInvoiceId = null)
        {
            ViewBag.Invoices = await _context.Invoices
                .Include(i => i.Patient).ThenInclude(p => p.User)
                .Where(i => i.Status != "Paid" || i.Id == selectedInvoiceId)
                .AsNoTracking()
                .OrderByDescending(i => i.IssuedAt)
                .ToListAsync();

            ViewBag.PaymentMethods = await _context.PaymentMethods
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync();

            ViewBag.SelectedInvoiceId = selectedInvoiceId;
        }
    }
}
