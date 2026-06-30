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
    public class SpecializationController : Controller
    {
        private readonly AppDbContext _context;

        public SpecializationController(AppDbContext context)
        {
            _context = context;
        }

        // List Specializations
        public IActionResult Index()
        {
            var specs = _context.Specializations
                .Include(s => s.Doctors).ThenInclude(d => d.User)
                .ToList();
            return View(specs);
        }

        // Details
        public IActionResult Details(int id)
        {
            var spec = _context.Specializations
                .Include(s => s.Doctors).ThenInclude(d => d.User)
                .FirstOrDefault(s => s.Id == id);

            if (spec == null)
                return NotFound();

            return View(spec);
        }

        // Create Specialization (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Specialization specialization)
        {
            if (ModelState.IsValid)
            {
                _context.Specializations.Add(specialization);
                _context.SaveChanges();
                TempData["Success"] = "Specialization added successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(specialization);
        }

        // Edit Specialization (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var spec = _context.Specializations.Find(id);
            if (spec == null)
                return NotFound();

            return View(spec);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Specialization specialization)
        {
            if (ModelState.IsValid)
            {
                _context.Update(specialization);
                _context.SaveChanges();
                TempData["Success"] = "Specialization updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(specialization);
        }

        // Delete Specialization (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var spec = _context.Specializations.Find(id);
            if (spec == null)
                return NotFound();

            return View(spec);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var spec = _context.Specializations.Find(id);
            if (spec == null)
                return NotFound();

            // Constraint checks
            if (_context.Doctors.Any(d => d.SpecializationId == id))
            {
                TempData["Error"] = "Cannot delete specialization because doctors are assigned to it.";
                return RedirectToAction(nameof(Index));
            }

            _context.Specializations.Remove(spec);
            _context.SaveChanges();
            TempData["Success"] = "Specialization deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
