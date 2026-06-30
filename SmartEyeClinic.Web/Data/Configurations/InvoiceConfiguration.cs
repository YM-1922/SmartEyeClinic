using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoices");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id)
                .UseIdentityColumn();

            builder.Property(i => i.AppointmentId)
                .IsRequired();

            builder.Property(i => i.PatientId)
                .IsRequired();

            builder.Property(i => i.InvoiceNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(i => i.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .IsRequired();

            builder.Property(i => i.PaidAmount)
                .HasColumnType("decimal(10, 2)")
                .HasDefaultValue(0.00m)
                .IsRequired(false);

            builder.Property(i => i.Tax)
                .HasColumnType("decimal(5, 2)")
                .HasDefaultValue(0.00m)
                .IsRequired(false);

            builder.Property(i => i.Discount)
                .HasColumnType("decimal(5, 2)")
                .HasDefaultValue(0.00m)
                .IsRequired(false);

            builder.Property(i => i.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(i => i.IssuedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired(false);

            // Indexes
            builder.HasIndex(i => i.PatientId)
                .HasDatabaseName("IX_Invoices_Patient");

            builder.HasIndex(i => i.AppointmentId)
                .IsUnique()
                .HasDatabaseName("IX_Invoices_Appointment");

            builder.HasIndex(i => i.InvoiceNumber)
                .IsUnique()
                .HasFilter("[InvoiceNumber] IS NOT NULL");

            // Relationships
            builder.HasOne(i => i.Appointment)
                .WithOne(a => a.Invoice)
                .HasForeignKey<Invoice>(i => i.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Invoices_Appointments");

            builder.HasOne(i => i.Patient)
                .WithMany(p => p.Invoices)
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Invoices_Patients");
        }
    }
}
