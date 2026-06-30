using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartEyeClinic.Models;
using System.IO;

#nullable enable

namespace SmartEyeClinic.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets for all 27 entities
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Branch> Branches { get; set; } = null!;
        public DbSet<Specialization> Specializations { get; set; } = null!;
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<DoctorBranch> DoctorBranches { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<PatientHistory> PatientHistories { get; set; } = null!;
        public DbSet<Receptionist> Receptionists { get; set; } = null!;
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; } = null!;
        public DbSet<DoctorReview> DoctorReviews { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<Queue> Queues { get; set; } = null!;
        public DbSet<Examination> Examinations { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<PrescriptionHeader> PrescriptionHeaders { get; set; } = null!;
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;
        public DbSet<SurgeryType> SurgeryTypes { get; set; } = null!;
        public DbSet<Surgery> Surgeries { get; set; } = null!;
        public DbSet<MedicalFile> MedicalFiles { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<InsuranceCompany> InsuranceCompanies { get; set; } = null!;
        public DbSet<PatientInsurance> PatientInsurances { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var basePath = Directory.GetCurrentDirectory();
                var builder = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                var configuration = builder.Build();
                var connectionString = configuration.GetConnectionString("DefaultConnection") 
                    ?? "Server=(localdb)\\MSSQLLocalDB;Database=SmartEyeClinic;Trusted_Connection=True;TrustServerCertificate=True;";

                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Register all configurations using Fluent API automatically
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
