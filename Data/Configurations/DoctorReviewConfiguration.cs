using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class DoctorReviewConfiguration : IEntityTypeConfiguration<DoctorReview>
    {
        public void Configure(EntityTypeBuilder<DoctorReview> builder)
        {
            builder.ToTable("DoctorReviews", t =>
            {
                t.HasCheckConstraint("CHK_Rating", "[Rating] BETWEEN 1 AND 5");
            });

            builder.HasKey(dr => dr.Id);

            builder.Property(dr => dr.Id)
                .UseIdentityColumn();

            builder.Property(dr => dr.DoctorId)
                .IsRequired();

            builder.Property(dr => dr.PatientId)
                .IsRequired();

            builder.Property(dr => dr.Rating)
                .IsRequired();

            builder.Property(dr => dr.Comment)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(dr => dr.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired(false);

            // Unique Constraint
            builder.HasIndex(dr => new { dr.DoctorId, dr.PatientId })
                .IsUnique()
                .HasDatabaseName("UQ_DoctorReviews");

            // Relationships
            builder.HasOne(dr => dr.Doctor)
                .WithMany(d => d.Reviews)
                .HasForeignKey(dr => dr.DoctorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DoctorReviews_Doctors");

            builder.HasOne(dr => dr.Patient)
                .WithMany(p => p.Reviews)
                .HasForeignKey(dr => dr.PatientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DoctorReviews_Patients");
        }
    }
}
