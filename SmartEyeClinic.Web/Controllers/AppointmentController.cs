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
        public async Task<IActionResult> Index()
        {
            var appointments = await _appointmentService.GetAllAppointmentsAsync();

            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim != null)
                {
                    int patId = int.Parse(patIdClaim);
                    appointments = appointments.Where(a => a.PatientId == patId).ToList();
                }
                else
                {
                    appointments = new List<Appointment>();
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim != null)
                {
                    int docId = int.Parse(docIdClaim);
                    appointments = appointments.Where(a => a.DoctorId == docId).ToList();
                }
                else
                {
                    appointments = new List<Appointment>();
                }
            }

            return View(appointments);
        }

        // Appointment Details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction(nameof(Index));
            }

            // IDOR Protection
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != appointment.PatientId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            return View(appointment);
        }

        // Create Appointment
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int patientId, 
            int doctorId, 
            int branchId, 
            DateTime appointmentDateTime, 
            int durationMinutes, 
            string type, 
            string status, 
            string? notes)
        {
            // Enforcement: Patient can only create appointments for themselves
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
                patientId = int.Parse(patIdClaim);
            }

            var result = await _appointmentService.AddAppointmentAsync(patientId, doctorId, branchId, appointmentDateTime, durationMinutes, type, status, notes);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await PopulateDropdownsAsync();
                return View();
            }
            TempData["Success"] = "Appointment booked successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Edit Appointment
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound();

            // IDOR Check
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != appointment.PatientId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            await PopulateDropdownsAsync(appointment.AppointmentDateTime, appointment.Id);
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
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
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound();

            // IDOR Check
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != appointment.PatientId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
                patientId = appointment.PatientId; // Patients can't shift patient ID
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            var result = await _appointmentService.UpdateAppointmentAsync(id, patientId, doctorId, branchId, appointmentDateTime, durationMinutes, type, status, notes);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                await PopulateDropdownsAsync(appointmentDateTime, id);
                return View(appointment);
            }
            TempData["Success"] = "Appointment updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Delete Appointment - GET
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction(nameof(Index));
            }

            // IDOR Check
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != appointment.PatientId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            return View(appointment);
        }

        // Delete Appointment - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction(nameof(Index));
            }

            // IDOR Check
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != appointment.PatientId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }
            else if (User.IsInRole("Doctor"))
            {
                var docIdClaim = User.FindFirst("DoctorId")?.Value;
                if (docIdClaim == null || int.Parse(docIdClaim) != appointment.DoctorId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            var result = await _appointmentService.DeleteAppointmentAsync(id);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            TempData["Success"] = "Appointment deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Receptionist,Doctor")]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _appointmentService.ApproveAppointmentAsync(id);
            if (result.Success)
            {
                TempData["Success"] = "Appointment approved successfully!";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);
                
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _appointmentService.RejectAppointmentAsync(id);
            if (result.Success)
            {
                TempData["Success"] = "Appointment request rejected.";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);
                
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Receptionist,Doctor")]
        public async Task<IActionResult> Complete(int id)
        {
            var result = await _appointmentService.CompleteAppointmentAsync(id);
            if (result.Success)
            {
                TempData["Success"] = "Appointment marked as completed successfully!";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);
                
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> PayDeposit(int id)
        {
            var result = await _appointmentService.PayDepositAsync(id);
            if (result.Success)
            {
                TempData["Success"] = "Deposit payment of $50.00 received! Request is now pending doctor approval.";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);
                
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Receptionist,Doctor")]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _appointmentService.UpdateAppointmentAsync(
                appointment.Id,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.BranchId,
                appointment.AppointmentDateTime,
                appointment.DurationMinutes,
                appointment.Type,
                "Cancelled",
                appointment.Notes);

            if (result.Success)
            {
                TempData["Success"] = "Appointment cancelled successfully!";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(DateTime? selectedDate = null, int? appointmentId = null)
        {
            ViewBag.Patients = await _context.Patients
                .Include(p => p.User)
                .AsNoTracking()
                .OrderBy(p => p.User.FullName)
                .ToListAsync();

            ViewBag.Doctors = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialization)
                .Include(d => d.Schedules)
                .AsNoTracking()
                .OrderBy(d => d.User.FullName)
                .ToListAsync();

            ViewBag.Branches = await _context.Branches
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();

            ViewBag.Specializations = await _context.Specializations
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}
