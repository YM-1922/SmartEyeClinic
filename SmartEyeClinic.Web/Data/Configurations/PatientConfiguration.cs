using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.ToTable("Patients");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .UseIdentityColumn();

            builder.Property(p => p.UserId)
                .IsRequired();

            builder.Property(p => p.NationalId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(p => p.DateOfBirth)
                .IsRequired(false);

            builder.Property(p => p.Gender)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(p => p.BloodType)
                .HasMaxLength(5)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(p => p.Address)
                .HasMaxLength(200)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(p => p.EmergencyContact)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(p => p.UserId)
                .IsUnique();

            builder.HasIndex(p => p.NationalId)
                .IsUnique()
                .HasFilter("[NationalId] IS NOT NULL");

            // Relationships
            builder.HasOne(p => p.User)
                .WithOne(u => u.Patient)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Patients_Users");
        }
    }
}
