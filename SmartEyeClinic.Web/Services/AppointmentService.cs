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

        public AppointmentService(AppDbContext context)
        {
            _context = context;
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
            if (!await _context.Patients.AnyAsync(p => p.Id == patientId))
                return ServiceResult.Fail("Patient not found!");

            if (!await _context.Doctors.AnyAsync(d => d.Id == doctorId))
                return ServiceResult.Fail("Doctor not found!");

            if (!await _context.Branches.AnyAsync(b => b.Id == branchId))
                return ServiceResult.Fail("Branch not found!");

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

            if (!await _context.Patients.AnyAsync(p => p.Id == patientId))
                return ServiceResult.Fail("Patient not found!");

            if (!await _context.Doctors.AnyAsync(d => d.Id == doctorId))
                return ServiceResult.Fail("Doctor not found!");

            if (!await _context.Branches.AnyAsync(b => b.Id == branchId))
                return ServiceResult.Fail("Branch not found!");

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

            appointment.PatientId = patientId;
            appointment.DoctorId = doctorId;
            appointment.BranchId = branchId;
            appointment.AppointmentDateTime = appointmentDateTime;
            appointment.DurationMinutes = durationMinutes;
            appointment.Type = type;
            appointment.Status = status;
            appointment.Notes = notes;

            await _context.SaveChangesAsync();
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
