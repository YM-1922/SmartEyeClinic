using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;
using SmartEyeClinic.Web.Services;

namespace SmartEyeClinic.Web.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly AppointmentService _appointmentService;
        private readonly AppDbContext _context;

        public AppointmentController(AppointmentService appointmentService, AppDbContext context)
        {
            _appointmentService = appointmentService;
            _context = context;
        }

        // GET: /Appointment/Index | استعراض قائمة المواعيد وتصفيتها حسب صلاحية المستخدم (طبيب أو مريض)
        [Authorize]
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

        // GET: /Appointment/Details/{id} | عرض تفاصيل موعد معين مع حماية الحسابات من الاختراق (IDOR Protection)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction(nameof(Index));
            }

            // حماية الوصول العشوائي وتفادي تخطي الصلاحيات
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

        // GET: /Appointment/Create | عرض صفحة حجز موعد جديد
        [HttpGet]
        public async Task<IActionResult> Create(int? doctorId)
        {
            await PopulateDropdownsAsync();
            ViewBag.SelectedDoctorId = doctorId;
            return View();
        }

        // POST: /Appointment/Create | معالجة طلب حجز موعد جديد، يدعم الحجز الفوري للزوار غير المسجلين وإنشاء حسابات تلقائية لهم
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
            string? notes,
            string? guestName,
            string? guestEmail,
            string? guestPhone,
            string? guestNationalId,
            DateTime? guestDOB,
            string? guestGender)
        {
            // التحقق من صحة بيانات الزائر في حال لم يكن المستخدم مسجلاً دخوله
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                if (string.IsNullOrWhiteSpace(guestName) || string.IsNullOrWhiteSpace(guestEmail) || 
                    string.IsNullOrWhiteSpace(guestPhone) || string.IsNullOrWhiteSpace(guestNationalId))
                {
                    TempData["Error"] = "Please fill in all patient registration fields for guest booking.";
                    await PopulateDropdownsAsync();
                    return View();
                }

                // التحقق من وجود مستخدم مسجل مسبقاً بنفس البريد الإلكتروني
                var existingUser = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == guestEmail);

                User targetUser;
                Patient targetPatient;

                if (existingUser != null)
                {
                    targetUser = existingUser;
                    var existingPatient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == targetUser.Id);
                    if (existingPatient == null)
                    {
                        // إنشاء ملف مريض للمستخدم المسجل مسبقاً
                        existingPatient = new Patient
                        {
                            UserId = targetUser.Id,
                            NationalId = guestNationalId,
                            DateOfBirth = guestDOB.HasValue ? DateOnly.FromDateTime(guestDOB.Value) : DateOnly.FromDateTime(DateTime.Today.AddYears(-30)),
                            Gender = guestGender ?? "Male"
                        };
                        _context.Patients.Add(existingPatient);
                        await _context.SaveChangesAsync();
                    }
                    targetPatient = existingPatient;
                }
                else
                {
                    // إنشاء حساب مستخدم مريض جديد للزائر
                    var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Patient");
                    targetUser = new User
                    {
                        FullName = guestName,
                        Email = guestEmail,
                        PasswordHash = "Patient@123", // كلمة مرور افتراضية مبدئية للمريض
                        PhoneNumber = guestPhone,
                        RoleId = patientRole?.Id ?? 3,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.Users.Add(targetUser);
                    await _context.SaveChangesAsync();

                    targetPatient = new Patient
                    {
                        UserId = targetUser.Id,
                        NationalId = guestNationalId,
                        DateOfBirth = guestDOB.HasValue ? DateOnly.FromDateTime(guestDOB.Value) : DateOnly.FromDateTime(DateTime.Today.AddYears(-30)),
                        Gender = guestGender ?? "Male"
                    };
                    _context.Patients.Add(targetPatient);
                    await _context.SaveChangesAsync();
                }

                patientId = targetPatient.Id;
                
                // تسجيل دخول المريض الجديد أو المسترد تلقائياً بعد الحجز
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, targetUser.FullName),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, targetUser.Email),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, targetUser.Id.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Patient"),
                    new System.Security.Claims.Claim("PatientId", targetPatient.Id.ToString())
                };

                var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(claimsIdentity));
            }
            else if (User.IsInRole("Patient"))
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
            TempData["Success"] = "Appointment booked successfully! You have been logged in automatically using default password: Patient@123";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Appointment/Edit/{id} | عرض صفحة تعديل موعد معين للتحرير
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound();

            // التحقق من الصلاحيات لمنع التلاعب بالمعرفات (IDOR Check)
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

        // POST: /Appointment/Edit/{id} | معالجة تحديث بيانات موعد معين بعد التحقق من الصلاحيات
        [Authorize]
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

            // حماية المريض من تغيير معرف المرضى الآخرين
            if (User.IsInRole("Patient"))
            {
                var patIdClaim = User.FindFirst("PatientId")?.Value;
                if (patIdClaim == null || int.Parse(patIdClaim) != appointment.PatientId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
                patientId = appointment.PatientId;
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

        // GET: /Appointment/Delete/{id} | عرض صفحة تأكيد حذف الموعد
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction(nameof(Index));
            }

            // التحقق من الصلاحيات لمنع التلاعب بالحذف (IDOR Check)
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

        // POST: /Appointment/Delete/{id} | تأكيد حذف موعد نهائياً من قاعدة البيانات
        [Authorize]
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

            // التحقق النهائي من الهوية قبل الحذف الإلزامي
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

        // POST: /Appointment/Approve/{id} | الموافقة على طلب الموعد وتأكيده
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

        // POST: /Appointment/Reject/{id} | رفض طلب الموعد
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

        // POST: /Appointment/Complete/{id} | وضع علامة مكتمل على الاستشارة الطبية بعد انتهائها
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

        // POST: /Appointment/PayDeposit/{id} | دفع مبلغ الوديعة المطلوب لتفعيل الموعد وبدء معالجة موافقة الطبيب
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

        // POST: /Appointment/Cancel/{id} | إلغاء الموعد المجدد
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

        // دالة مساعدة لتعبئة القوائم المنسدلة (المرضى، الأطباء، الفروع، التخصصات) في النماذج
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
