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
    public class PaymentController : Controller
    {
        private readonly PaymentService _paymentService;
        private readonly AppDbContext _context;

        public PaymentController(PaymentService paymentService, AppDbContext context)
        {
            _paymentService = paymentService;
            _context = context;
        }

        // List Payments
        public IActionResult Index()
        {
            var payments = _paymentService.GetAllPayments();
            return View(payments);
        }

        // Payment Details
        [HttpGet]
        public IActionResult Details(int id)
        {
            var payment = _paymentService.GetPaymentById(id);
            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(payment);
        }

        // Create Payment
        [HttpGet]
        public IActionResult Create(int? invoiceId = null)
        {
            PopulateDropdowns(invoiceId);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int invoiceId, int paymentMethodId, decimal amount, string? transactionRef)
        {
            var result = _paymentService.AddPayment(invoiceId, paymentMethodId, amount, transactionRef);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                PopulateDropdowns(invoiceId);
                return View();
            }
            TempData["Success"] = "Payment registered successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Delete Payment (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var payment = _paymentService.GetPaymentById(id);
            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(payment);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var result = _paymentService.DeletePayment(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Payment reversed successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Payment Methods listing
        public IActionResult Methods()
        {
            var methods = _paymentService.GetAllPaymentMethods();
            return View(methods);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult AddMethod()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMethod(string name)
        {
            var result = _paymentService.AddPaymentMethod(name);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View();
            }
            TempData["Success"] = "Payment method added successfully!";
            return RedirectToAction(nameof(Methods));
        }

        private void PopulateDropdowns(int? selectedInvoiceId = null)
        {
            ViewBag.Invoices = _context.Invoices
                .Include(i => i.Patient).ThenInclude(p => p.User)
                .Where(i => i.Status != "Paid" || i.Id == selectedInvoiceId)
                .OrderByDescending(i => i.IssuedAt)
                .ToList();

            ViewBag.PaymentMethods = _context.PaymentMethods
                .OrderBy(m => m.Name)
                .ToList();

            ViewBag.SelectedInvoiceId = selectedInvoiceId;
        }
    }
}
