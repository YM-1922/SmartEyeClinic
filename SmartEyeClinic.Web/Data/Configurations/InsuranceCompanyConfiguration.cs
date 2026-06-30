using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class InsuranceCompanyConfiguration : IEntityTypeConfiguration<InsuranceCompany>
    {
        public void Configure(EntityTypeBuilder<InsuranceCompany> builder)
        {
            builder.ToTable("InsuranceCompanies");

            builder.HasKey(ic => ic.Id);

            builder.Property(ic => ic.Id)
                .UseIdentityColumn();

            builder.Property(ic => ic.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            builder.Property(ic => ic.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(ic => ic.Address)
                .HasMaxLength(200)
                .IsUnicode(false)
                .IsRequired(false);
        }
    }
}
