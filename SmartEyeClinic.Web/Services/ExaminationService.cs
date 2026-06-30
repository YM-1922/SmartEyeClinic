using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Services
{
    public class ExaminationService
    {
        private readonly AppDbContext _context;

        public ExaminationService(AppDbContext context)
        {
            _context = context;
        }

        // Get All Examinations
        public List<Examination> GetAllExaminations()
        {
            return _context.Examinations
                .Include(e => e.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .OrderByDescending(e => e.ExaminedAt)
                .ToList();
        }

        // Get Examination By Id
        public Examination? GetExaminationById(int id)
        {
            return _context.Examinations
                .Include(e => e.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(e => e.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User)
                .FirstOrDefault(e => e.Id == id);
        }

        // Add Examination
        public ServiceResult AddExamination(
            int appointmentId, 
            string diagnosis, 
            string? symptoms,
            string? visualAcuityLeft, 
            string? visualAcuityRight, 
            string? intraocularPressure, 
            string? treatmentPlan)
        {
            if (string.IsNullOrWhiteSpace(diagnosis))
                return ServiceResult.Fail("Diagnosis description is required.");

            if (!_context.Appointments.Any(a => a.Id == appointmentId))
                return ServiceResult.Fail("Appointment not found!");

            var examination = new Examination
            {
                AppointmentId = appointmentId,
                Diagnosis = diagnosis,
                Symptoms = symptoms,
                VisualAcuityLeft = visualAcuityLeft,
                VisualAcuityRight = visualAcuityRight,
                IntraocularPressure = intraocularPressure,
                TreatmentPlan = treatmentPlan,
                ExaminedAt = DateTime.Now
            };

            _context.Examinations.Add(examination);

            // Automatically set the corresponding Appointment status to 'Completed' upon examination
            var appointment = _context.Appointments.Find(appointmentId);
            if (appointment != null)
            {
                appointment.Status = "Completed";
            }

            _context.SaveChanges();
            return ServiceResult.Ok();
        }

        // Update Examination
        public ServiceResult UpdateExamination(
            int id,
            int appointmentId,
            string diagnosis,
            string? symptoms,
            string? visualAcuityLeft,
            string? visualAcuityRight,
            string? intraocularPressure,
            string? treatmentPlan)
        {
            var exam = _context.Examinations.Find(id);
            if (exam == null)
                return ServiceResult.Fail("Examination record not found.");

            if (string.IsNullOrWhiteSpace(diagnosis))
                return ServiceResult.Fail("Diagnosis description is required.");

            if (!_context.Appointments.Any(a => a.Id == appointmentId))
                return ServiceResult.Fail("Appointment not found!");

            exam.AppointmentId = appointmentId;
            exam.Diagnosis = diagnosis;
            exam.Symptoms = symptoms;
            exam.VisualAcuityLeft = visualAcuityLeft;
            exam.VisualAcuityRight = visualAcuityRight;
            exam.IntraocularPressure = intraocularPressure;
            exam.TreatmentPlan = treatmentPlan;

            _context.SaveChanges();
            return ServiceResult.Ok();
        }

        // Delete Examination
        public ServiceResult DeleteExamination(int id)
        {
            var exam = _context.Examinations.Find(id);
            if (exam == null)
                return ServiceResult.Fail("Examination record not found.");

            // Check if patient has dependent prescriptions generated from this examination
            if (_context.PrescriptionHeaders.Any(ph => ph.ExaminationId == id))
                return ServiceResult.Fail("Cannot delete this examination because prescriptions have been issued based on it.");

            _context.Examinations.Remove(exam);
            _context.SaveChanges();
            return ServiceResult.Ok();
        }
    }
}
