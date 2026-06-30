using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .UseIdentityColumn();

            builder.Property(p => p.InvoiceId)
                .IsRequired();

            builder.Property(p => p.PaymentMethodId)
                .IsRequired();

            builder.Property(p => p.Amount)
                .HasColumnType("decimal(10, 2)")
                .IsRequired();

            builder.Property(p => p.TransactionRef)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(p => p.PaidAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired(false);

            // Relationships
            builder.HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Payments_Invoice");

            builder.HasOne(p => p.PaymentMethod)
                .WithMany(pm => pm.Payments)
                .HasForeignKey(p => p.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Payments_Method");
        }
    }
}
