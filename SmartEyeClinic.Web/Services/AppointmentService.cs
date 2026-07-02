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
                Status = status,
                Notes = notes,
                CreatedAt = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Notify users
            var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == patientId);
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == doctorId);
            if (patient != null && doctor != null)
            {
                await _notificationService.CreateNotificationAsync(patient.User.Id, "Appointment Requested", $"Your appointment with Dr. {doctor.User.FullName} is registered and pending approval.", "Info");
                await _notificationService.CreateNotificationAsync(doctor.User.Id, "New Request Received", $"New appointment booking request from patient {patient.User.FullName} for {appointmentDateTime:f}.", "Request");
            }

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
    }
}
