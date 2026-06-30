using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SmartEyeClinic.Data;
using SmartEyeClinic.Models;

namespace SmartEyeClinic.Web.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // 1. Seed Roles
            if (!context.Roles.Any())
            {
                var roles = new[]
                {
                    new Role { Name = "Admin", Description = "System Administrator with full access" },
                    new Role { Name = "Doctor", Description = "Medical Specialists" },
                    new Role { Name = "Patient", Description = "Clinic Patients" },
                    new Role { Name = "Receptionist", Description = "Front Desk and Receptionist staff" }
                };
                context.Roles.AddRange(roles);
                context.SaveChanges();
            }

            // Get role references
            var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
            var doctorRole = context.Roles.FirstOrDefault(r => r.Name == "Doctor");
            var patientRole = context.Roles.FirstOrDefault(r => r.Name == "Patient");
            var receptionistRole = context.Roles.FirstOrDefault(r => r.Name == "Receptionist");

            // 2. Seed Branches
            if (!context.Branches.Any())
            {
                var branches = new[]
                {
                    new Branch { Name = "Main Downtown Branch", Address = "123 Medical Center Ave, Downtown", Phone = "555-0100" },
                    new Branch { Name = "North Medical Plaza", Address = "456 Skyline Drive, North District", Phone = "555-0200" },
                    new Branch { Name = "West Eye Center", Address = "789 Sunset Blvd, Westside", Phone = "555-0300" }
                };
                context.Branches.AddRange(branches);
                context.SaveChanges();
            }
            var mainBranch = context.Branches.FirstOrDefault();

            // 3. Seed Specializations
            if (!context.Specializations.Any())
            {
                var specializations = new[]
                {
                    new Specialization { Name = "General Ophthalmology", Description = "Comprehensive eye examinations and basic care" },
                    new Specialization { Name = "Cornea & External Disease", Description = "Corneal transplants and complex anterior segment care" },
                    new Specialization { Name = "Retina & Vitreous", Description = "Macular degeneration, diabetic retinopathy, and retinal detachment" },
                    new Specialization { Name = "Pediatric Ophthalmology", Description = "Strabismus and pediatric eye conditions" },
                    new Specialization { Name = "Refractive Surgery & LASIK", Description = "Vision correction procedures" }
                };
                context.Specializations.AddRange(specializations);
                context.SaveChanges();
            }
            var generalSpec = context.Specializations.FirstOrDefault();

            // 4. Seed Payment Methods
            if (!context.PaymentMethods.Any())
            {
                var methods = new[]
                {
                    new PaymentMethod { Name = "Cash" },
                    new PaymentMethod { Name = "Credit Card" },
                    new PaymentMethod { Name = "Insurance Claim" },
                    new PaymentMethod { Name = "Bank Transfer" }
                };
                context.PaymentMethods.AddRange(methods);
                context.SaveChanges();
            }

            // 5. Seed Medicines
            if (!context.Medicines.Any())
            {
                var medicines = new[]
                {
                    new Medicine { Name = "Artificial Tears Drops", Description = "Lubricant eye drops for dry eye relief", Manufacturer = "OcuCare Labs" },
                    new Medicine { Name = "Tobramycin Ophthalmic 0.3%", Description = "Antibiotic eye drops for bacterial infections", Manufacturer = "PharmaOptics" },
                    new Medicine { Name = "Latanoprost 0.005% Drops", Description = "Prostaglandin analog for glaucoma pressure reduction", Manufacturer = "GlaucoMed" },
                    new Medicine { Name = "Timolol Maleate 0.5%", Description = "Beta-blocker eye drops for ocular hypertension", Manufacturer = "BetaShield" },
                    new Medicine { Name = "Pataday Olopatadine 0.2%", Description = "Antihistamine drops for ocular allergies", Manufacturer = "Alcon" }
                };
                context.Medicines.AddRange(medicines);
                context.SaveChanges();
            }

            // 6. Seed Surgery Types
            if (!context.SurgeryTypes.Any())
            {
                var surgeryTypes = new[]
                {
                    new SurgeryType { Name = "Phacoemulsification (Cataract)", Description = "Ultrasound removal of cloudy lens and intraocular lens implantation" },
                    new SurgeryType { Name = "LASIK Vision Correction", Description = "Laser resurfacing of cornea to correct myopia/astigmatism" },
                    new SurgeryType { Name = "Trabeculectomy (Glaucoma)", Description = "Creating a new drainage pathway to lower eye pressure" },
                    new SurgeryType { Name = "Vitrectomy", Description = "Removal of vitreous gel to treat retinal tears or diabetic bleeding" }
                };
                context.SurgeryTypes.AddRange(surgeryTypes);
                context.SaveChanges();
            }

            // 7. Seed Users (Admin, Doctor, Patient, Receptionist)
            if (!context.Users.Any())
            {
                // Admin User
                var adminUser = new User
                {
                    FullName = "Admin Principal",
                    Email = "admin@smarteye.com",
                    PasswordHash = "admin123", // plaintext verification for ease
                    PhoneNumber = "555-9000",
                    RoleId = adminRole?.Id ?? 1,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(adminUser);

                // Doctor User & Profile
                var doctorUser = new User
                {
                    FullName = "Dr. Alexander Wright",
                    Email = "doctor@smarteye.com",
                    PasswordHash = "doctor123",
                    PhoneNumber = "555-9001",
                    RoleId = doctorRole?.Id ?? 2,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(doctorUser);
                context.SaveChanges(); // Save to get User ID

                var doctorProfile = new Doctor
                {
                    UserId = doctorUser.Id,
                    SpecializationId = generalSpec?.Id ?? 1,
                    LicenseNumber = "LIC-9988-OPH",
                    ConsultationFee = 150.00m,
                    Bio = "Dr. Alexander Wright has over 15 years of experience in clinical ophthalmology specializing in general eye health and advanced diagnostics."
                };
                context.Doctors.Add(doctorProfile);

                // Doctor schedule
                var schedule = new DoctorSchedule
                {
                    Doctor = doctorProfile,
                    DayOfWeek = "Monday",
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    IsAvailable = true
                };
                context.DoctorSchedules.Add(schedule);

                // Receptionist User & Profile
                var receptionistUser = new User
                {
                    FullName = "Sarah Connor",
                    Email = "receptionist@smarteye.com",
                    PasswordHash = "recep123",
                    PhoneNumber = "555-9002",
                    RoleId = receptionistRole?.Id ?? 4,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(receptionistUser);
                context.SaveChanges(); // Save to get User ID

                var receptionistProfile = new Receptionist
                {
                    UserId = receptionistUser.Id,
                    BranchId = mainBranch?.Id ?? 1,
                    ShiftStart = new TimeOnly(8, 0),
                    ShiftEnd = new TimeOnly(16, 0)
                };
                context.Receptionists.Add(receptionistProfile);

                // Patient User & Profile
                var patientUser = new User
                {
                    FullName = "John Doe",
                    Email = "patient@smarteye.com",
                    PasswordHash = "patient123",
                    PhoneNumber = "555-9003",
                    RoleId = patientRole?.Id ?? 3,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(patientUser);
                context.SaveChanges(); // Save to get User ID

                var patientProfile = new Patient
                {
                    UserId = patientUser.Id,
                    NationalId = "NAT-11223344",
                    Gender = "Male",
                    Address = "742 Evergreen Terrace, Springfield",
                    DateOfBirth = new DateOnly(1985, 6, 15)
                };
                context.Patients.Add(patientProfile);

                context.SaveChanges();
            }
        }
    }
}
