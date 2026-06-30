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
        public List<Appointment> GetAllAppointments()
        {
            return _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Branch)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToList();
        }

        // Get Appointment By Id
        public Appointment? GetAppointmentById(int id)
        {
            return _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Branch)
                .FirstOrDefault(a => a.Id == id);
        }

        // Add Appointment with Full Details
        public ServiceResult AddAppointment(
            int patientId, 
            int doctorId, 
            int branchId, 
            DateTime appointmentDateTime, 
            int durationMinutes, 
            string type, 
            string status, 
            string? notes)
        {
            if (!_context.Patients.Any(p => p.Id == patientId))
                return ServiceResult.Fail("Patient not found!");

            if (!_context.Doctors.Any(d => d.Id == doctorId))
                return ServiceResult.Fail("Doctor not found!");

            if (!_context.Branches.Any(b => b.Id == branchId))
                return ServiceResult.Fail("Branch not found!");

            // Double booking validation (Doctor)
            var endTime = appointmentDateTime.AddMinutes(durationMinutes);
            bool docBusy = _context.Appointments.Any(a => 
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
            _context.SaveChanges();

            return ServiceResult.Ok();
        }

        // Update Appointment
        public ServiceResult UpdateAppointment(
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
            var appointment = _context.Appointments.Find(id);
            if (appointment == null)
                return ServiceResult.Fail("Appointment not found!");

            if (!_context.Patients.Any(p => p.Id == patientId))
                return ServiceResult.Fail("Patient not found!");

            if (!_context.Doctors.Any(d => d.Id == doctorId))
                return ServiceResult.Fail("Doctor not found!");

            if (!_context.Branches.Any(b => b.Id == branchId))
                return ServiceResult.Fail("Branch not found!");

            // Check doctor conflict for other appointments
            var endTime = appointmentDateTime.AddMinutes(durationMinutes);
            bool docBusy = _context.Appointments.Any(a => 
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

            _context.SaveChanges();
            return ServiceResult.Ok();
        }

        // Delete Appointment
        public ServiceResult DeleteAppointment(int id)
        {
            var appointment = _context.Appointments.Find(id);
            if (appointment == null)
                return ServiceResult.Fail("Appointment not found!");

            // Check constraints
            if (_context.Examinations.Any(e => e.AppointmentId == id))
                return ServiceResult.Fail("Cannot delete appointment because it has clinical examination records associated.");

            if (_context.Invoices.Any(i => i.AppointmentId == id))
                return ServiceResult.Fail("Cannot delete appointment because it has associated billing invoices.");

            if (_context.Queues.Any(q => q.AppointmentId == id))
            {
                var qRecords = _context.Queues.Where(q => q.AppointmentId == id);
                _context.Queues.RemoveRange(qRecords);
            }

            _context.Appointments.Remove(appointment);
            _context.SaveChanges();
            return ServiceResult.Ok();
        }
    }
}
