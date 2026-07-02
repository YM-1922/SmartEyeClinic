using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services
{
    public class AppointmentService
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;

        public AppointmentService(AppDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // Get All Appointments
        public async Task<List<Appointment>> GetAllAppointmentsAsync()
        {
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Branch)
                .AsNoTracking()
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();
        }

        // Get Appointment By Id
        public async Task<Appointment?> GetAppointmentByIdAsync(int id)
        {
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        // Add Appointment with Full Details
        public async Task<ServiceResult> AddAppointmentAsync(
            int patientId, 
            int doctorId, 
            int branchId, 
            DateTime appointmentDateTime, 
            int durationMinutes, 
            string type, 
            string status, 
            string? notes)
        {
            if (appointmentDateTime <= DateTime.Now)
                return ServiceResult.Fail("Appointment date and time must be in the future!");

            if (!await _context.Patients.AnyAsync(p => p.Id == patientId))
                return ServiceResult.Fail("Patient not found!");

            if (!await _context.Doctors.AnyAsync(d => d.Id == doctorId))
                return ServiceResult.Fail("Doctor not found!");

            if (!await _context.Branches.AnyAsync(b => b.Id == branchId))
                return ServiceResult.Fail("Branch not found!");

            // Doctor schedule validation
            var dayOfWeekName = appointmentDateTime.DayOfWeek.ToString();
            var timeOfDay = TimeOnly.FromDateTime(appointmentDateTime);
            
            var matchingSchedule = await _context.DoctorSchedules
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeekName && s.IsAvailable == true);
                
            if (matchingSchedule == null)
            {
                var workDays = await _context.DoctorSchedules
                    .Where(s => s.DoctorId == doctorId && s.IsAvailable == true)
                    .Select(s => s.DayOfWeek)
                    .ToListAsync();
                
                string daysStr = workDays.Any() ? string.Join(", ", workDays) : "No days scheduled";
                return ServiceResult.Fail($"The selected doctor is not available on {dayOfWeekName}. Scheduled working days: {daysStr}");
            }
            
            if (timeOfDay < matchingSchedule.StartTime || timeOfDay.AddMinutes(durationMinutes) > matchingSchedule.EndTime)
            {
                return ServiceResult.Fail($"The selected time is outside the doctor's working hours on {dayOfWeekName} ({matchingSchedule.StartTime:hh\\:mm\\ tt} - {matchingSchedule.EndTime:hh\\:mm\\ tt}).");
            }

            // Double booking validation (Doctor)
            var endTime = appointmentDateTime.AddMinutes(durationMinutes);
            bool docBusy = await _context.Appointments.AnyAsync(a => 
                a.DoctorId == doctorId && 
                a.Status != "Cancelled" &&
                ((a.AppointmentDateTime <= appointmentDateTime && appointmentDateTime < a.AppointmentDateTime.AddMinutes(a.DurationMinutes)) ||
                 (a.AppointmentDateTime < endTime && endTime <= a.AppointmentDateTime.AddMinutes(a.DurationMinutes))));
            
            if (docBusy)
                return ServiceResult.Fail("The selected doctor already has a scheduled appointment during this time window.");

            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = doctorId,
                BranchId = branchId,
                AppointmentDateTime = appointmentDateTime,
                DurationMinutes = durationMinutes,
                Type = type,
                Status = "Pending Deposit",
                Notes = notes,
                CreatedAt = DateTime.Now,
                DepositAmount = 50.00m,
                DepositStatus = "Pending"
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Notify users
            var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == patientId);
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == doctorId);
            if (patient != null && doctor != null)
            {
                await _notificationService.CreateNotificationAsync(patient.User.Id, "Appointment Created", $"Your appointment with Dr. {doctor.User.FullName} has been registered.", "Info");
                await _notificationService.CreateNotificationAsync(patient.User.Id, "Deposit Required", $"A deposit payment of $50.00 is required to request doctor approval.", "Warning");
            }
            await _notificationService.NotifyAllAdminsAsync("New Appointment", $"New appointment request created for patient {patient?.User?.FullName}.", "Info");
            await _notificationService.NotifyAllReceptionistsAsync("New Appointment Booked", $"New appointment request booked for patient {patient?.User?.FullName}.", "Info");

            return ServiceResult.Ok();
        }

        // Update Appointment
        public async Task<ServiceResult> UpdateAppointmentAsync(
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
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return ServiceResult.Fail("Appointment not found!");

            if (appointmentDateTime <= DateTime.Now)
                return ServiceResult.Fail("Appointment date and time must be in the future!");

            if (!await _context.Patients.AnyAsync(p => p.Id == patientId))
                return ServiceResult.Fail("Patient not found!");

            if (!await _context.Doctors.AnyAsync(d => d.Id == doctorId))
                return ServiceResult.Fail("Doctor not found!");

            if (!await _context.Branches.AnyAsync(b => b.Id == branchId))
                return ServiceResult.Fail("Branch not found!");

            // Doctor schedule validation
            var dayOfWeekName = appointmentDateTime.DayOfWeek.ToString();
            var timeOfDay = TimeOnly.FromDateTime(appointmentDateTime);
            
            var matchingSchedule = await _context.DoctorSchedules
                .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeekName && s.IsAvailable == true);
                
            if (matchingSchedule == null)
            {
                var workDays = await _context.DoctorSchedules
                    .Where(s => s.DoctorId == doctorId && s.IsAvailable == true)
                    .Select(s => s.DayOfWeek)
                    .ToListAsync();
                
                string daysStr = workDays.Any() ? string.Join(", ", workDays) : "No days scheduled";
                return ServiceResult.Fail($"The selected doctor is not available on {dayOfWeekName}. Scheduled working days: {daysStr}");
            }
            
            if (timeOfDay < matchingSchedule.StartTime || timeOfDay.AddMinutes(durationMinutes) > matchingSchedule.EndTime)
            {
                return ServiceResult.Fail($"The selected time is outside the doctor's working hours on {dayOfWeekName} ({matchingSchedule.StartTime:hh\\:mm\\ tt} - {matchingSchedule.EndTime:hh\\:mm\\ tt}).");
            }

            // Check doctor conflict for other appointments
            var endTime = appointmentDateTime.AddMinutes(durationMinutes);
            bool docBusy = await _context.Appointments.AnyAsync(a => 
                a.Id != id &&
                a.DoctorId == doctorId && 
                a.Status != "Cancelled" &&
                ((a.AppointmentDateTime <= appointmentDateTime && appointmentDateTime < a.AppointmentDateTime.AddMinutes(a.DurationMinutes)) ||
                 (a.AppointmentDateTime < endTime && endTime <= a.AppointmentDateTime.AddMinutes(a.DurationMinutes))));

            if (docBusy)
                return ServiceResult.Fail("The selected doctor already has a scheduled appointment during this time window.");

            var oldStatus = appointment.Status;
            var oldDateTime = appointment.AppointmentDateTime;

            appointment.PatientId = patientId;
            appointment.DoctorId = doctorId;
            appointment.BranchId = branchId;
            appointment.AppointmentDateTime = appointmentDateTime;
            appointment.DurationMinutes = durationMinutes;
            appointment.Type = type;
            appointment.Status = status;
            appointment.Notes = notes;

            await _context.SaveChangesAsync();

            // Notify users of changes
            var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == patientId);
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == doctorId);
            if (patient != null && doctor != null)
            {
                if (oldStatus != status)
                {
                    string title = $"Appointment {status}";
                    string message = $"Your appointment with Dr. {doctor.User.FullName} on {appointmentDateTime:f} is now {status}.";
                    await _notificationService.CreateNotificationAsync(patient.User.Id, title, message, "StatusUpdate");
                    await _notificationService.CreateNotificationAsync(doctor.User.Id, title, $"Appointment with {patient.User.FullName} status updated to {status}.", "StatusUpdate");
                }
                else if (oldDateTime != appointmentDateTime)
                {
                    string title = "Appointment Rescheduled";
                    string message = $"Your appointment with Dr. {doctor.User.FullName} has been rescheduled to {appointmentDateTime:f}.";
                    await _notificationService.CreateNotificationAsync(patient.User.Id, title, message, "Rescheduled");
                    await _notificationService.CreateNotificationAsync(doctor.User.Id, title, $"Appointment with {patient.User.FullName} rescheduled to {appointmentDateTime:f}.", "Rescheduled");
                }
            }

            return ServiceResult.Ok();
        }

        // Delete Appointment
        public async Task<ServiceResult> DeleteAppointmentAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return ServiceResult.Fail("Appointment not found!");

            // Check constraints
            if (await _context.Examinations.AnyAsync(e => e.AppointmentId == id))
                return ServiceResult.Fail("Cannot delete appointment because it has clinical examination records associated.");

            if (await _context.Invoices.AnyAsync(i => i.AppointmentId == id))
                return ServiceResult.Fail("Cannot delete appointment because it has associated billing invoices.");

            if (await _context.Queues.AnyAsync(q => q.AppointmentId == id))
            {
                var qRecords = _context.Queues.Where(q => q.AppointmentId == id);
                _context.Queues.RemoveRange(qRecords);
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        // Pay Deposit
        public async Task<ServiceResult> PayDepositAsync(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (appointment == null)
                return ServiceResult.Fail("Appointment not found!");

            if (appointment.DepositStatus == "Paid")
                return ServiceResult.Fail("Deposit has already been paid!");

            appointment.DepositStatus = "Paid";
            appointment.PaymentDate = DateTime.Now;
            appointment.Status = "Pending Doctor Approval";

            // Create Invoice
            var invoice = new Invoice
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                InvoiceNumber = $"INV-{DateTime.Now.Ticks}",
                TotalAmount = appointment.DepositAmount,
                PaidAmount = appointment.DepositAmount,
                Status = "Paid",
                IssuedAt = DateTime.Now
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Create Payment
            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                PaymentMethodId = 1, // Default method (e.g. Card)
                Amount = appointment.DepositAmount,
                TransactionRef = $"TXN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                PaidAt = DateTime.Now
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Notify Patient
            if (appointment.Patient?.User != null)
            {
                await _notificationService.CreateNotificationAsync(appointment.Patient.User.Id, "Deposit Received", $"We have received your deposit of ${appointment.DepositAmount:F2} for your appointment on {appointment.AppointmentDateTime:f}.", "Success");
            }
            // Notify Doctor
            if (appointment.Doctor?.User != null)
            {
                await _notificationService.CreateNotificationAsync(appointment.Doctor.User.Id, "New Request Received", $"Deposit paid. New appointment request from {appointment.Patient?.User?.FullName} for {appointment.AppointmentDateTime:f} is ready for your review.", "Info");
            }
            // Notify Receptionist & Admin
            await _notificationService.NotifyAllReceptionistsAsync("Deposit Paid", $"Deposit of ${appointment.DepositAmount:F2} paid by patient {appointment.Patient?.User?.FullName}.", "Info");
            await _notificationService.NotifyAllAdminsAsync("Payment Activity", $"Deposit of ${appointment.DepositAmount:F2} paid for appointment #{appointment.Id}.", "Payment");

            return ServiceResult.Ok();
        }

        // Approve Appointment
        public async Task<ServiceResult> ApproveAppointmentAsync(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return ServiceResult.Fail("Appointment not found!");

            if (appointment.DepositStatus != "Paid")
                return ServiceResult.Fail("Appointment cannot be approved because the deposit has not been paid!");

            appointment.Status = "Approved";
            await _context.SaveChangesAsync();

            // Notify Patient
            if (appointment.Patient?.User != null)
            {
                await _notificationService.CreateNotificationAsync(appointment.Patient.User.Id, "Appointment Approved", $"Your appointment with Dr. {appointment.Doctor?.User?.FullName} has been approved.", "Success");
            }
            // Notify Receptionist
            await _notificationService.NotifyAllReceptionistsAsync("Appointment Approved", $"Appointment #{appointment.Id} approved by Dr. {appointment.Doctor?.User?.FullName}.", "Info");

            return ServiceResult.Ok();
        }

        // Reject Appointment
        public async Task<ServiceResult> RejectAppointmentAsync(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return ServiceResult.Fail("Appointment not found!");

            appointment.Status = "Rejected";
            await _context.SaveChangesAsync();

            // Notify Patient
            if (appointment.Patient?.User != null)
            {
                await _notificationService.CreateNotificationAsync(appointment.Patient.User.Id, "Appointment Rejected", "Your appointment request was rejected. Please choose another available time.", "Warning");
            }

            return ServiceResult.Ok();
        }

        // Complete Appointment
        public async Task<ServiceResult> CompleteAppointmentAsync(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return ServiceResult.Fail("Appointment not found!");

            appointment.Status = "Completed";
            await _context.SaveChangesAsync();

            // Notify Patient
            if (appointment.Patient?.User != null)
            {
                await _notificationService.CreateNotificationAsync(appointment.Patient.User.Id, "Appointment Completed", "Your appointment has been marked as completed. Thank you for choosing Smart Eye Clinic.", "Success");
            }

            return ServiceResult.Ok();
        }
    }
}
