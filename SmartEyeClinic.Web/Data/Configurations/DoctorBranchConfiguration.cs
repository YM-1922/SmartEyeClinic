using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class DoctorBranchConfiguration : IEntityTypeConfiguration<DoctorBranch>
    {
        public void Configure(EntityTypeBuilder<DoctorBranch> builder)
        {
            builder.ToTable("DoctorBranches");

            builder.HasKey(db => new { db.DoctorId, db.BranchId })
                .HasName("PK_DoctorBranches");

            // Relationships
            builder.HasOne(db => db.Doctor)
                .WithMany(d => d.DoctorBranches)
                .HasForeignKey(db => db.DoctorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DoctorBranches_Doctors");

            builder.HasOne(db => db.Branch)
                .WithMany(b => b.DoctorBranches)
                .HasForeignKey(db => db.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DoctorBranches_Branches");
        }
    }
}
