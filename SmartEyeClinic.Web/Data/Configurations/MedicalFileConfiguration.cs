using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class MedicalFileConfiguration : IEntityTypeConfiguration<MedicalFile>
    {
        public void Configure(EntityTypeBuilder<MedicalFile> builder)
        {
            builder.ToTable("MedicalFiles");

            builder.HasKey(mf => mf.Id);

            builder.Property(mf => mf.Id)
                .UseIdentityColumn();

            builder.Property(mf => mf.PatientId)
                .IsRequired();

            builder.Property(mf => mf.AppointmentId)
                .IsRequired(false);

            builder.Property(mf => mf.UploadedBy)
                .IsRequired(false);

            builder.Property(mf => mf.FileType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(mf => mf.FilePath)
                .HasMaxLength(300)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(mf => mf.FileSize)
                .IsRequired(false);

            builder.Property(mf => mf.UploadedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired(false);

            // Relationships
            builder.HasOne(mf => mf.Patient)
                .WithMany(p => p.MedicalFiles)
                .HasForeignKey(mf => mf.PatientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MedicalFiles_Patients");

            builder.HasOne(mf => mf.Appointment)
                .WithMany(a => a.MedicalFiles)
                .HasForeignKey(mf => mf.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MedicalFiles_Appointments");

            builder.HasOne(mf => mf.Uploader)
                .WithMany(u => u.MedicalFilesUploaded)
                .HasForeignKey(mf => mf.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MedicalFiles_Users");
        }
    }
}
