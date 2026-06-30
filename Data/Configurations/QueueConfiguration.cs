using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class QueueConfiguration : IEntityTypeConfiguration<Queue>
    {
        public void Configure(EntityTypeBuilder<Queue> builder)
        {
            builder.ToTable("Queue");

            builder.HasKey(q => q.Id);

            builder.Property(q => q.Id)
                .UseIdentityColumn();

            builder.Property(q => q.AppointmentId)
                .IsRequired();

            builder.Property(q => q.QueueNumber)
                .IsRequired();

            builder.Property(q => q.Priority)
                .HasDefaultValue(0)
                .IsRequired(false);

            builder.Property(q => q.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(q => q.CheckInTime)
                .IsRequired(false);

            builder.Property(q => q.CalledAt)
                .IsRequired(false);

            builder.Property(q => q.EstimatedTime)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(q => q.AppointmentId)
                .IsUnique();

            builder.HasIndex(q => q.QueueNumber)
                .HasDatabaseName("IX_Queue_Number");

            // Relationships
            builder.HasOne(q => q.Appointment)
                .WithOne(a => a.Queue)
                .HasForeignKey<Queue>(q => q.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Queue_Appointments");
        }
    }
}
