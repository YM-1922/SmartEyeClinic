using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Controllers
{
    [Authorize]
    public class BranchController : Controller
    {
        private readonly AppDbContext _context;

        public BranchController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Branch/Index | استعراض فروع العيادة الطبية وحساباتها
        public async Task<IActionResult> Index()
        {
            var branches = await _context.Branches
                .Include(b => b.Appointments)
                .Include(b => b.Receptionists)
                .AsNoTracking()
                .ToListAsync();
            return View(branches);
        }

        // GET: /Branch/Details/{id} | استعراض تفاصيل فرع العيادة وتصفية المواعيد بالنسبة للمرضى
        public async Task<IActionResult> Details(int id)
        {
            var branch = await _context.Branches
                .Include(b => b.Appointments).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(b => b.Appointments).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .Include(b => b.Receptionists).ThenInclude(r => r.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null)
                return NotFound();

            // حماية خصوصية المرضى ومنع تداخل الحسابات
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim != null)
                {
                    int patId = int.Parse(patIdClaim);
                    branch.Appointments = branch.Appointments.Where(a => a.PatientId == patId).ToList();
                }
                else
                {
                    branch.Appointments = new List<Appointment>();
                }
            }

            return View(branch);
        }

        // GET: /Branch/Create | عرض صفحة إضافة فرع عيادة جديد للمدير
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Branch/Create | معالجة طلب إضافة فرع جديد وحفظه
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Branches.Add(branch);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Branch created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        // GET: /Branch/Edit/{id} | عرض صفحة تعديل بيانات فرع معين
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return NotFound();

            return View(branch);
        }

        // POST: /Branch/Edit/{id} | معالجة تحديث بيانات الفرع في قاعدة البيانات
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Update(branch);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Branch updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        // GET: /Branch/Delete/{id} | عرض صفحة تأكيد حذف الفرع
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return NotFound();

            return View(branch);
        }

        // POST: /Branch/Delete/{id} | معالجة الحذف النهائي لفرع العيادة بعد التأكد من قيود المواعيد والاستقبال
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return NotFound();

            // التحقق من سلامة القيود المرجعية قبل الحذف (وجود مواعيد أو موظفي استقبال)
            if (await _context.Appointments.AnyAsync(a => a.BranchId == id))
            {
                TempData["Error"] = "Cannot delete branch because it has associated patient appointments scheduled.";
                return RedirectToAction(nameof(Index));
            }

            if (await _context.Receptionists.AnyAsync(r => r.BranchId == id))
            {
                TempData["Error"] = "Cannot delete branch because receptionists are currently assigned to this location.";
                return RedirectToAction(nameof(Index));
            }

            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Branch deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
