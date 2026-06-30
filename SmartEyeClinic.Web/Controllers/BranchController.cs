using System;
using System.Linq;
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

        // List Branches
        public IActionResult Index()
        {
            var branches = _context.Branches
                .Include(b => b.Appointments)
                .Include(b => b.Receptionists)
                .ToList();
            return View(branches);
        }

        // Details of a Branch
        public IActionResult Details(int id)
        {
            var branch = _context.Branches
                .Include(b => b.Appointments).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(b => b.Appointments).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .Include(b => b.Receptionists).ThenInclude(r => r.User)
                .FirstOrDefault(b => b.Id == id);

            if (branch == null)
                return NotFound();

            return View(branch);
        }

        // Create Branch (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Branches.Add(branch);
                _context.SaveChanges();
                TempData["Success"] = "Branch created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        // Edit Branch (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var branch = _context.Branches.Find(id);
            if (branch == null)
                return NotFound();

            return View(branch);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Update(branch);
                _context.SaveChanges();
                TempData["Success"] = "Branch updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        // Delete Branch (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var branch = _context.Branches.Find(id);
            if (branch == null)
                return NotFound();

            return View(branch);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var branch = _context.Branches.Find(id);
            if (branch == null)
                return NotFound();

            // Constraint checks
            if (_context.Appointments.Any(a => a.BranchId == id))
            {
                TempData["Error"] = "Cannot delete branch because it has associated patient appointments scheduled.";
                return RedirectToAction(nameof(Index));
            }

            if (_context.Receptionists.Any(r => r.BranchId == id))
            {
                TempData["Error"] = "Cannot delete branch because receptionists are currently assigned to this location.";
                return RedirectToAction(nameof(Index));
            }

            _context.Branches.Remove(branch);
            _context.SaveChanges();
            TempData["Success"] = "Branch deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
