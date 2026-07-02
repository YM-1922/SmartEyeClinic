using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace SmartEyeClinic.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public IActionResult Landing()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction(nameof(Index));
        }
        return View();
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(AdminDashboard));
            if (User.IsInRole("Doctor"))
                return RedirectToAction(nameof(DoctorDashboard));
            if (User.IsInRole("Patient"))
                return RedirectToAction(nameof(PatientDashboard));
            if (User.IsInRole("Receptionist"))
                return RedirectToAction(nameof(ReceptionistDashboard));
        }

        return RedirectToAction(nameof(Landing));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminDashboard()
    {
        // System Statistics
        ViewBag.TotalPatients = await _context.Patients.CountAsync();
        ViewBag.TotalDoctors = await _context.Doctors.CountAsync();
        ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
        ViewBag.TotalRevenue = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;
        ViewBag.TotalSurgeries = await _context.Surgeries.CountAsync();
        ViewBag.TodayAppointmentsCount = await _context.Appointments.CountAsync(a => a.AppointmentDateTime.Date == DateTime.Today);
        ViewBag.UnpaidInvoicesCount = await _context.Invoices.CountAsync(i => i.Status != "Paid");

        // Recent Audit Logs
        var auditLogs = await _context.AuditLogs
            .Include(al => al.User)
            .OrderByDescending(al => al.CreatedAt)
            .Take(6)
            .ToListAsync();
        ViewBag.RecentAudits = auditLogs;

        // Recent Payments
        var recentPayments = await _context.Payments
            .Include(p => p.Invoice).ThenInclude(i => i.Patient).ThenInclude(pat => pat.User)
            .Include(p => p.PaymentMethod)
            .OrderByDescending(p => p.PaidAt)
            .Take(5)
            .ToListAsync();
        ViewBag.RecentPayments = recentPayments;

        // System Notifications
        ViewBag.RecentNotifications = await _context.Notifications
            .OrderByDescending(n => n.SentAt)
            .Take(6)
            .ToListAsync();

        return View();
    }

    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> DoctorDashboard()
    {
        var docIdClaim = User.FindFirst("DoctorId")?.Value;
        if (string.IsNullOrEmpty(docIdClaim))
            return RedirectToAction("AccessDenied", "Account");

        int doctorId = int.Parse(docIdClaim);

        // Fetch Doctor today's appointments
        var todayAppointments = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Branch)
            .Where(a => a.DoctorId == doctorId && a.AppointmentDateTime.Date == DateTime.Today)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
        ViewBag.TodayAppointments = todayAppointments;

        // Stats for Doctor
        ViewBag.TotalMyPatients = await _context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .Select(a => a.PatientId)
            .Distinct()
            .CountAsync();
        ViewBag.MyPendingAppointments = await _context.Appointments
            .CountAsync(a => a.DoctorId == doctorId && a.Status == "Pending" && a.AppointmentDateTime.Date >= DateTime.Today);
        ViewBag.MySurgeries = await _context.Surgeries
            .CountAsync(s => s.DoctorId == doctorId);

        // Upcoming surgeries
        var upcomingSurgeries = await _context.Surgeries
            .Include(s => s.Patient).ThenInclude(p => p.User)
            .Include(s => s.SurgeryType)
            .Where(s => s.DoctorId == doctorId && s.SurgeryDate >= DateTime.Today)
            .OrderBy(s => s.SurgeryDate)
            .Take(5)
            .ToListAsync();
        ViewBag.UpcomingSurgeries = upcomingSurgeries;

        // Doctor's Notifications
        var doctorUserClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(doctorUserClaim))
        {
            int docUserId = int.Parse(doctorUserClaim);
            ViewBag.RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == docUserId)
                .OrderByDescending(n => n.SentAt)
                .Take(6)
                .ToListAsync();
        }

        return View();
    }

    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> PatientDashboard()
    {
        var patIdClaim = User.FindFirst("PatientId")?.Value;
        if (string.IsNullOrEmpty(patIdClaim))
            return RedirectToAction("AccessDenied", "Account");

        int patientId = int.Parse(patIdClaim);

        // Retrieve patient
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        // Fetch upcoming appointments
        var upcomingAppointments = await _context.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Branch)
            .Where(a => a.PatientId == patientId && a.AppointmentDateTime >= DateTime.Now && a.Status != "Cancelled")
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
        ViewBag.UpcomingAppointments = upcomingAppointments;

        // Fetch prescriptions
        var prescriptions = await _context.PrescriptionHeaders
            .Include(ph => ph.Examination).ThenInclude(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
            .Include(ph => ph.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .Where(ph => ph.Examination.Appointment.PatientId == patientId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(5)
            .ToListAsync();
        ViewBag.Prescriptions = prescriptions;

        // Fetch Invoices
        var invoices = await _context.Invoices
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.IssuedAt)
            .Take(5)
            .ToListAsync();
        ViewBag.Invoices = invoices;

        // Patient's Notifications
        var patientUserClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(patientUserClaim))
        {
            int patUserId = int.Parse(patientUserClaim);
            ViewBag.RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == patUserId)
                .OrderByDescending(n => n.SentAt)
                .Take(6)
                .ToListAsync();
        }

        return View(patient);
    }

    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> ReceptionistDashboard()
    {
        // Real-time Queue
        var queue = await _context.Queues
            .Include(q => q.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
            .Include(q => q.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
            .Where(q => q.CheckInTime.HasValue && q.CheckInTime.Value.Date == DateTime.Today && q.Status != "Completed")
            .OrderBy(q => q.QueueNumber)
            .ToListAsync();
        ViewBag.ActiveQueue = queue;

        // Today's appointments for receptionist to manage
        var todayApps = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Where(a => a.AppointmentDateTime.Date == DateTime.Today)
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();
        ViewBag.TodayAppointments = todayApps;

        // Stats
        ViewBag.QueueTotal = await _context.Queues.CountAsync(q => q.CheckInTime.HasValue && q.CheckInTime.Value.Date == DateTime.Today);
        ViewBag.QueueWaiting = await _context.Queues.CountAsync(q => q.CheckInTime.HasValue && q.CheckInTime.Value.Date == DateTime.Today && q.Status == "Waiting");
        ViewBag.UnpaidInvoices = await _context.Invoices.CountAsync(i => i.Status == "Unpaid");

        // Receptionist's Notifications
        var recepUserClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(recepUserClaim))
        {
            int recepUserId = int.Parse(recepUserClaim);
            ViewBag.RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == recepUserId)
                .OrderByDescending(n => n.SentAt)
                .Take(6)
                .ToListAsync();
        }

        return View();
    }

    public IActionResult Profile()
    {
        return RedirectToAction("Profile", "Account");
    }

    public IActionResult PatientPortal()
    {
        return RedirectToAction("Index"); // Redirects to proper patient dashboard
    }

    [Authorize]
    public IActionResult Calendar()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

