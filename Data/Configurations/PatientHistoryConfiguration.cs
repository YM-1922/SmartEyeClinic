using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class PatientHistoryConfiguration : IEntityTypeConfiguration<PatientHistory>
    {
        public void Configure(EntityTypeBuilder<PatientHistory> builder)
        {
            builder.ToTable("PatientHistory");

            builder.HasKey(ph => ph.Id);

            builder.Property(ph => ph.Id)
                .UseIdentityColumn();

            builder.Property(ph => ph.PatientId)
                .IsRequired();

            builder.Property(ph => ph.DiseaseName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(ph => ph.DiagnosedDate)
                .IsRequired(false);

            builder.Property(ph => ph.Notes)
                .HasMaxLength(500)
                .IsUnicode(false)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(ph => ph.PatientId)
                .HasDatabaseName("IX_PatientHistory_Patient");

            // Relationships
            builder.HasOne(ph => ph.Patient)
                .WithMany(p => p.Histories)
                .HasForeignKey(ph => ph.PatientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PatientHistory_Patients");
        }
    }
}
