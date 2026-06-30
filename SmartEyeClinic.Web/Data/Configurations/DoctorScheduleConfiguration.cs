using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
    {
        public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
        {
            builder.ToTable("DoctorSchedules");

            builder.HasKey(ds => ds.Id);

            builder.Property(ds => ds.Id)
                .UseIdentityColumn();

            builder.Property(ds => ds.DoctorId)
                .IsRequired();

            builder.Property(ds => ds.DayOfWeek)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(ds => ds.StartTime)
                .IsRequired();

            builder.Property(ds => ds.EndTime)
                .IsRequired();

            builder.Property(ds => ds.IsAvailable)
                .HasDefaultValue(true)
                .IsRequired(false);

            // Relationships
            builder.HasOne(ds => ds.Doctor)
                .WithMany(d => d.Schedules)
                .HasForeignKey(ds => ds.DoctorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DoctorSchedules_Doctors");
        }
    }
}
