using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class PatientInsuranceConfiguration : IEntityTypeConfiguration<PatientInsurance>
    {
        public void Configure(EntityTypeBuilder<PatientInsurance> builder)
        {
            builder.ToTable("PatientInsurance");

            builder.HasKey(pi => pi.Id);

            builder.Property(pi => pi.Id)
                .UseIdentityColumn();

            builder.Property(pi => pi.PatientId)
                .IsRequired();

            builder.Property(pi => pi.InsuranceCompanyId)
                .IsRequired();

            builder.Property(pi => pi.InsuranceNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(pi => pi.StartDate)
                .IsRequired(false);

            builder.Property(pi => pi.EndDate)
                .IsRequired(false);

            // Relationships
            builder.HasOne(pi => pi.Patient)
                .WithMany(p => p.Insurances)
                .HasForeignKey(pi => pi.PatientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PatientInsurance_Patients");

            builder.HasOne(pi => pi.InsuranceCompany)
                .WithMany(ic => ic.PatientInsurances)
                .HasForeignKey(pi => pi.InsuranceCompanyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PatientInsurance_Companies");
        }
    }
}
