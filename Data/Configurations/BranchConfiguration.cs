using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.ToTable("Branches");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Id)
                .UseIdentityColumn();

            builder.Property(b => b.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(b => b.Address)
                .HasMaxLength(200)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(b => b.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired(false);
        }
    }
}
