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

            Console.WriteLine("SEED_LOG: Starting Database Seeding Routine...");

            // 1. Seed Roles
            var roles = new[]
            {
                new Role { Name = "Admin", Description = "System Administrator with full access" },
                new Role { Name = "Doctor", Description = "Medical Specialists" },
                new Role { Name = "Patient", Description = "Clinic Patients" },
                new Role { Name = "Receptionist", Description = "Front Desk and Receptionist staff" }
            };

            foreach (var r in roles)
            {
                if (!context.Roles.Any(x => x.Name == r.Name))
                {
                    context.Roles.Add(r);
                    Console.WriteLine($"SEED_LOG: [CREATE] Role: {r.Name}");
                }
                else
                {
                    Console.WriteLine($"SEED_LOG: [SKIP] Role: {r.Name} (already exists)");
                }
            }
            context.SaveChanges();

            // Get role references
            var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
            var doctorRole = context.Roles.FirstOrDefault(r => r.Name == "Doctor");
            var patientRole = context.Roles.FirstOrDefault(r => r.Name == "Patient");
            var receptionistRole = context.Roles.FirstOrDefault(r => r.Name == "Receptionist");

            // 2. Seed Branches
            var branches = new[]
            {
                new Branch { Name = "Main Downtown Branch", Address = "123 Medical Center Ave, Downtown", Phone = "555-0100" },
                new Branch { Name = "North Medical Plaza", Address = "456 Skyline Drive, North District", Phone = "555-0200" },
                new Branch { Name = "West Eye Center", Address = "789 Sunset Blvd, Westside", Phone = "555-0300" }
            };

            foreach (var b in branches)
            {
                if (!context.Branches.Any(x => x.Name == b.Name))
                {
                    context.Branches.Add(b);
                    Console.WriteLine($"SEED_LOG: [CREATE] Branch: {b.Name}");
                }
                else
                {
                    Console.WriteLine($"SEED_LOG: [SKIP] Branch: {b.Name} (already exists)");
                }
            }
            context.SaveChanges();
            var mainBranch = context.Branches.FirstOrDefault(b => b.Name == "Main Downtown Branch") ?? context.Branches.FirstOrDefault();

            // 3. Seed Specializations
            var specsToSeed = new[]
            {
                ("Retina Specialist", "Specializes in vitreoretinal diseases, macular degeneration, and retinal surgery"),
                ("Cataract Specialist", "Specializes in clouding of natural lens and premium lens implants"),
                ("Glaucoma Specialist", "Specializes in high intraocular pressure and optic nerve management"),
                ("Cornea Specialist", "Specializes in corneal diseases, transplants, and complex segment care"),
                ("Pediatric Ophthalmologist", "Specializes in pediatric vision issues and strabismus"),
                ("General Ophthalmologist", "Provides comprehensive clinical eye screenings and basic care"),
                ("Refractive Surgery Specialist", "Specializes in laser vision correction, LASIK, and PRK procedures"),
                ("Oculoplastic Specialist", "Specializes in cosmetic and reconstructive eyelid/orbital surgery"),
                ("Neuro Ophthalmologist", "Specializes in visual system problems related to nervous system"),
                ("Contact Lens Specialist", "Specializes in precision medical and corrective contact lens fitting")
            };

            foreach (var s in specsToSeed)
            {
                if (!context.Specializations.Any(x => x.Name == s.Item1))
                {
                    context.Specializations.Add(new Specialization { Name = s.Item1, Description = s.Item2 });
                    Console.WriteLine($"SEED_LOG: [CREATE] Specialization: {s.Item1}");
                }
                else
                {
                    Console.WriteLine($"SEED_LOG: [SKIP] Specialization: {s.Item1} (already exists)");
                }
            }
            context.SaveChanges();

            // 4. Seed Payment Methods
            var methods = new[]
            {
                new PaymentMethod { Name = "Cash" },
                new PaymentMethod { Name = "Credit Card" },
                new PaymentMethod { Name = "Insurance Claim" },
                new PaymentMethod { Name = "Bank Transfer" }
            };

            foreach (var m in methods)
            {
                if (!context.PaymentMethods.Any(x => x.Name == m.Name))
                {
                    context.PaymentMethods.Add(m);
                    Console.WriteLine($"SEED_LOG: [CREATE] Payment Method: {m.Name}");
                }
                else
                {
                    Console.WriteLine($"SEED_LOG: [SKIP] Payment Method: {m.Name} (already exists)");
                }
            }
            context.SaveChanges();

            // 5. Seed Medicines
            var medicines = new[]
            {
                new Medicine { Name = "Artificial Tears Drops", Description = "Lubricant eye drops for dry eye relief", Manufacturer = "OcuCare Labs" },
                new Medicine { Name = "Tobramycin Ophthalmic 0.3%", Description = "Antibiotic eye drops for bacterial infections", Manufacturer = "PharmaOptics" },
                new Medicine { Name = "Latanoprost 0.005% Drops", Description = "Prostaglandin analog for glaucoma pressure reduction", Manufacturer = "GlaucoMed" },
                new Medicine { Name = "Timolol Maleate 0.5%", Description = "Beta-blocker eye drops for ocular hypertension", Manufacturer = "BetaShield" },
                new Medicine { Name = "Pataday Olopatadine 0.2%", Description = "Antihistamine drops for ocular allergies", Manufacturer = "Alcon" }
            };

            foreach (var med in medicines)
            {
                if (!context.Medicines.Any(x => x.Name == med.Name))
                {
                    context.Medicines.Add(med);
                    Console.WriteLine($"SEED_LOG: [CREATE] Medicine: {med.Name}");
                }
                else
                {
                    Console.WriteLine($"SEED_LOG: [SKIP] Medicine: {med.Name} (already exists)");
                }
            }
            context.SaveChanges();

            // 6. Seed Surgery Types
            var surgeryTypes = new[]
            {
                new SurgeryType { Name = "Phacoemulsification (Cataract)", Description = "Ultrasound removal of cloudy lens and intraocular lens implantation" },
                new SurgeryType { Name = "LASIK Vision Correction", Description = "Laser resurfacing of cornea to correct myopia/astigmatism" },
                new SurgeryType { Name = "Trabeculectomy (Glaucoma)", Description = "Creating a new drainage pathway to lower eye pressure" },
                new SurgeryType { Name = "Vitrectomy", Description = "Removal of vitreous gel to treat retinal tears or diabetic bleeding" }
            };

            foreach (var st in surgeryTypes)
            {
                if (!context.SurgeryTypes.Any(x => x.Name == st.Name))
                {
                    context.SurgeryTypes.Add(st);
                    Console.WriteLine($"SEED_LOG: [CREATE] Surgery Type: {st.Name}");
                }
                else
                {
                    Console.WriteLine($"SEED_LOG: [SKIP] Surgery Type: {st.Name} (already exists)");
                }
            }
            context.SaveChanges();

            // 7. Seed Admin User
            var adminUser = GetOrCreateUser(context, "Admin Principal", "admin@smarteye.com", "admin123", "555-9000", adminRole?.Id ?? 1);

            // 8. Seed Receptionist User & Profile
            var receptionistUser = GetOrCreateUser(context, "Sarah Connor", "receptionist@smarteye.com", "recep123", "555-9100", receptionistRole?.Id ?? 4);
            GetOrCreateReceptionist(context, receptionistUser.Id, mainBranch?.Id ?? 1, new TimeOnly(8, 0), new TimeOnly(16, 0));

            // 9. Seed Patient User & Profile
            var patientUser = GetOrCreateUser(context, "John Doe", "patient@smarteye.com", "patient123", "555-9200", patientRole?.Id ?? 3);
            GetOrCreatePatient(context, patientUser.Id, "NAT-11223344", "Male", "742 Evergreen Terrace, Springfield", new DateOnly(1985, 6, 15));

            // 10. Seed the 10 Doctor Accounts
            var doctorsToSeed = new[]
            {
                new { Name = "Dr. Alexander Wright", Email = "doctor1@smarteye.com", Phone = "555-9001", License = "LIC-ALEX-OPH", Specialty = "General Ophthalmologist", Bio = "Dr. Alexander Wright is highly experienced in general clinical ophthalmology and diagnostics.", Days = new[]{"Monday", "Friday"}, Start = 9, End = 17 },
                new { Name = "Dr. Clara Oswald", Email = "doctor2@smarteye.com", Phone = "555-9002", License = "LIC-CLARA-OPH", Specialty = "Cornea Specialist", Bio = "Dr. Clara Oswald is an expert in corneal transplants and refractive anterior therapies.", Days = new[]{"Tuesday"}, Start = 10, End = 16 },
                new { Name = "Dr. Bruce Banner", Email = "doctor3@smarteye.com", Phone = "555-9003", License = "LIC-BRUCE-OPH", Specialty = "Retina Specialist", Bio = "Dr. Bruce Banner specializes in macular degeneration and vitreoretinal operations.", Days = new[]{"Wednesday"}, Start = 8, End = 15 },
                new { Name = "Dr. Diana Prince", Email = "doctor4@smarteye.com", Phone = "555-9004", License = "LIC-DIANA-OPH", Specialty = "Pediatric Ophthalmologist", Bio = "Dr. Diana Prince is dedicated to pediatric strabismus and childhood vision checkups.", Days = new[]{"Thursday"}, Start = 9, End = 14 },

            };

            foreach (var docInfo in doctorsToSeed)
            {
                var docUser = GetOrCreateUser(context, docInfo.Name, docInfo.Email, "Doctor@123", docInfo.Phone, doctorRole?.Id ?? 2);
                var spec = context.Specializations.FirstOrDefault(s => s.Name == docInfo.Specialty);
                var fee = 150.00m + (docUser.Id % 5) * 10;
                var doctor = GetOrCreateDoctor(context, docUser.Id, spec?.Id ?? 1, docInfo.License, fee, docInfo.Bio);

                foreach (var day in docInfo.Days)
                {
                    CreateDoctorScheduleIfNotExists(context, doctor.Id, day, new TimeOnly(docInfo.Start, 0), new TimeOnly(docInfo.End, 0));
                }

                // Print account to Seed Log console for developers to easily find
                Console.WriteLine($"SEED_LOG: Seeding Doctor Profile Complete: {docInfo.Name} | Email: {docInfo.Email} | Password: Doctor@123 | Specialty: {docInfo.Specialty}");
            }

            // 11. Seed Legacy Doctor User & Profile
            var legacyDocUser = GetOrCreateUser(context, "Dr. Alexander Wright Legacy", "doctor@smarteye.com", "doctor123", "555-9999", doctorRole?.Id ?? 2);
            var generalSpec = context.Specializations.FirstOrDefault(s => s.Name == "General Ophthalmologist") ?? context.Specializations.FirstOrDefault();
            GetOrCreateDoctor(context, legacyDocUser.Id, generalSpec?.Id ?? 1, "LIC-LEGACY-OPH", 150.00m, "Legacy account for testing.");

            Console.WriteLine("SEED_LOG: Database Seeding Routine Completed Successfully.");
        }

        private static User GetOrCreateUser(AppDbContext context, string fullName, string email, string passwordHash, string phoneNumber, int roleId)
        {
            // 1. Check by email
            var user = context.Users.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                Console.WriteLine($"SEED_LOG: [SKIP] User creation: {email} (exists by email)");
                return user;
            }

            // 2. Check by phone number
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                user = context.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);
                if (user != null)
                {
                    Console.WriteLine($"SEED_LOG: [SKIP] User creation: {fullName} (exists by phone {phoneNumber}). Mapping to existing user email: {user.Email}");
                    return user;
                }
            }

            // 3. Create new user
            user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = passwordHash,
                PhoneNumber = phoneNumber,
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(user);
            context.SaveChanges();
            Console.WriteLine($"SEED_LOG: [CREATE] User: {email} (phone: {phoneNumber})");
            return user;
        }

        private static Receptionist GetOrCreateReceptionist(AppDbContext context, int userId, int branchId, TimeOnly shiftStart, TimeOnly shiftEnd)
        {
            var receptionist = context.Receptionists.FirstOrDefault(r => r.UserId == userId);
            if (receptionist != null)
            {
                Console.WriteLine($"SEED_LOG: [SKIP] Receptionist Profile for UserId: {userId} (already exists)");
                return receptionist;
            }

            receptionist = new Receptionist
            {
                UserId = userId,
                BranchId = branchId,
                ShiftStart = shiftStart,
                ShiftEnd = shiftEnd
            };
            context.Receptionists.Add(receptionist);
            context.SaveChanges();
            Console.WriteLine($"SEED_LOG: [CREATE] Receptionist Profile for UserId: {userId}");
            return receptionist;
        }

        private static Patient GetOrCreatePatient(AppDbContext context, int userId, string nationalId, string gender, string address, DateOnly dateOfBirth)
        {
            var patient = context.Patients.FirstOrDefault(p => p.UserId == userId);
            if (patient != null)
            {
                Console.WriteLine($"SEED_LOG: [SKIP] Patient Profile for UserId: {userId} (already exists)");
                return patient;
            }

            // Verify NationalId is not taken
            if (!string.IsNullOrEmpty(nationalId))
            {
                var existingByNat = context.Patients.FirstOrDefault(p => p.NationalId == nationalId);
                if (existingByNat != null)
                {
                    var newNationalId = $"NAT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                    Console.WriteLine($"SEED_LOG: [WARNING] Patient NationalId {nationalId} already taken by UserId {existingByNat.UserId}. Re-generating unique NationalId: {newNationalId}");
                    nationalId = newNationalId;
                }
            }

            patient = new Patient
            {
                UserId = userId,
                NationalId = nationalId,
                Gender = gender,
                Address = address,
                DateOfBirth = dateOfBirth
            };
            context.Patients.Add(patient);
            context.SaveChanges();
            Console.WriteLine($"SEED_LOG: [CREATE] Patient Profile for UserId: {userId} (NationalId: {nationalId})");
            return patient;
        }

        private static Doctor GetOrCreateDoctor(AppDbContext context, int userId, int specializationId, string licenseNumber, decimal consultationFee, string bio)
        {
            var doctor = context.Doctors.FirstOrDefault(d => d.UserId == userId);
            if (doctor != null)
            {
                Console.WriteLine($"SEED_LOG: [SKIP] Doctor Profile for UserId: {userId} (already exists)");
                return doctor;
            }

            // Verify LicenseNumber is not taken
            var existingByLic = context.Doctors.FirstOrDefault(d => d.LicenseNumber == licenseNumber);
            if (existingByLic != null)
            {
                var newLicense = $"LIC-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}-OPH";
                Console.WriteLine($"SEED_LOG: [WARNING] Doctor LicenseNumber {licenseNumber} already taken by doctor ID {existingByLic.Id}. Re-generating unique LicenseNumber: {newLicense}");
                licenseNumber = newLicense;
            }

            doctor = new Doctor
            {
                UserId = userId,
                SpecializationId = specializationId,
                LicenseNumber = licenseNumber,
                ConsultationFee = consultationFee,
                Bio = bio
            };
            context.Doctors.Add(doctor);
            context.SaveChanges();
            Console.WriteLine($"SEED_LOG: [CREATE] Doctor Profile for UserId: {userId} (License: {licenseNumber})");
            return doctor;
        }

        private static void CreateDoctorScheduleIfNotExists(AppDbContext context, int doctorId, string dayOfWeek, TimeOnly startTime, TimeOnly endTime)
        {
            var exists = context.DoctorSchedules.Any(ds => ds.DoctorId == doctorId && ds.DayOfWeek == dayOfWeek);
            if (exists)
            {
                Console.WriteLine($"SEED_LOG: [SKIP] Schedule for DoctorId: {doctorId} on {dayOfWeek} (already exists)");
                return;
            }

            var schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                IsAvailable = true
            };
            context.DoctorSchedules.Add(schedule);
            context.SaveChanges();
            Console.WriteLine($"SEED_LOG: [CREATE] Schedule for DoctorId: {doctorId} on {dayOfWeek}");
        }
    }
}
