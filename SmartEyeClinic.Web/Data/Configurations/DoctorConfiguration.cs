using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.ToTable("Doctors");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Id)
                .UseIdentityColumn();

            builder.Property(d => d.UserId)
                .IsRequired();

            builder.Property(d => d.SpecializationId)
                .IsRequired();

            builder.Property(d => d.LicenseNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(d => d.ConsultationFee)
                .HasColumnType("decimal(10, 2)")
                .IsRequired();

            builder.Property(d => d.Bio)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(d => d.UserId)
                .IsUnique();

            builder.HasIndex(d => d.LicenseNumber)
                .IsUnique();

            // Relationships
            builder.HasOne(d => d.User)
                .WithOne(u => u.Doctor)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Doctors_Users");

            builder.HasOne(d => d.Specialization)
                .WithMany(s => s.Doctors)
                .HasForeignKey(d => d.SpecializationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Doctors_Specializations");
        }
    }
}
