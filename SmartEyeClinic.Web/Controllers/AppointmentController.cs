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
    public class AppointmentController : Controller
    {
        private readonly AppointmentService _appointmentService;
        private readonly AppDbContext _context;

        public AppointmentController(AppointmentService appointmentService, AppDbContext context)
        {
            _appointmentService = appointmentService;
            _context = context;
        }

        // List Appointments
        public IActionResult Index()
        {
            var appointments = _appointmentService.GetAllAppointments();
            return View(appointments);
        }

        // Appointment Details
        [HttpGet]
        public IActionResult Details(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(appointment);
        }

        // Create Appointment
        [HttpGet]
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            int patientId, 
            int doctorId, 
            int branchId, 
            DateTime appointmentDateTime, 
            int durationMinutes, 
            string type, 
            string status, 
            string? notes)
        {
            var result = _appointmentService.AddAppointment(patientId, doctorId, branchId, appointmentDateTime, durationMinutes, type, status, notes);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                PopulateDropdowns();
                return View();
            }
            TempData["Success"] = "Appointment booked successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Edit Appointment
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment == null)
                return NotFound();

            PopulateDropdowns();
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            int id,
            int patientId,
            int doctorId,
            int branchId,
            DateTime appointmentDateTime,
            int durationMinutes,
            string type,
            string status,
            string? notes)
        {
            var result = _appointmentService.UpdateAppointment(id, patientId, doctorId, branchId, appointmentDateTime, durationMinutes, type, status, notes);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                PopulateDropdowns();
                var appointment = _appointmentService.GetAppointmentById(id);
                return View(appointment);
            }
            TempData["Success"] = "Appointment updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Delete Appointment - GET
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var appointment = _appointmentService.GetAppointmentById(id);
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(appointment);
        }

        // Delete Appointment - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var result = _appointmentService.DeleteAppointment(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Appointment deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns()
        {
            ViewBag.Patients = _context.Patients.Include(p => p.User).OrderBy(p => p.User.FullName).ToList();
            ViewBag.Doctors = _context.Doctors.Include(d => d.User).OrderBy(d => d.User.FullName).ToList();
            ViewBag.Branches = _context.Branches.OrderBy(b => b.Name).ToList();
        }
    }
}
