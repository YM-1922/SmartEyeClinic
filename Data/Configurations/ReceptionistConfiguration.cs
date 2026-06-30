using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class ReceptionistConfiguration : IEntityTypeConfiguration<Receptionist>
    {
        public void Configure(EntityTypeBuilder<Receptionist> builder)
        {
            builder.ToTable("Receptionists");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .UseIdentityColumn();

            builder.Property(r => r.UserId)
                .IsRequired();

            builder.Property(r => r.BranchId)
                .IsRequired();

            builder.Property(r => r.ShiftStart)
                .IsRequired(false);

            builder.Property(r => r.ShiftEnd)
                .IsRequired(false);

            // Indexes
            builder.HasIndex(r => r.UserId)
                .IsUnique();

            // Relationships
            builder.HasOne(r => r.User)
                .WithOne(u => u.Receptionist)
                .HasForeignKey<Receptionist>(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Receptionists_Users");

            builder.HasOne(r => r.Branch)
                .WithMany(b => b.Receptionists)
                .HasForeignKey(r => r.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Receptionists_Branches");
        }
    }
}
