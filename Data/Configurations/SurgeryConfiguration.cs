using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class SurgeryConfiguration : IEntityTypeConfiguration<Surgery>
    {
        public void Configure(EntityTypeBuilder<Surgery> builder)
        {
            builder.ToTable("Surgeries");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .UseIdentityColumn();


            builder.Property(s => s.AppointmentId)
                .IsRequired();

            builder.Property(s => s.SurgeryTypeId)
                .IsRequired();

            builder.Property(s => s.SurgeryDate)
                .IsRequired();

            builder.Property(s => s.Outcome)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(s => s.Notes)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(s => s.AppointmentId)
                .IsUnique();

          

            builder.HasOne(s => s.Appointment)
                .WithOne(a => a.Surgery)
                .HasForeignKey<Surgery>(s => s.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Surgeries_Appointments");

            builder.HasOne(s => s.SurgeryType)
                .WithMany(st => st.Surgeries)
                .HasForeignKey(s => s.SurgeryTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Surgeries_Types");
        }
    }
}
