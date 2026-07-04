using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEyeClinic.Models;
using System;

#nullable enable

namespace SmartEyeClinic.Data.Configurations
{
    public class ReviewReplyConfiguration : IEntityTypeConfiguration<ReviewReply>
    {
        public void Configure(EntityTypeBuilder<ReviewReply> builder)
        {
            builder.ToTable("ReviewReplies");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .UseIdentityColumn();

            builder.Property(r => r.Content)
                .HasMaxLength(1000)
                .IsUnicode(true)
                .IsRequired();

            builder.Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired();

            // Relationships
            builder.HasOne(r => r.Review)
                .WithMany(rev => rev.Replies)
                .HasForeignKey(r => r.ReviewId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ReviewReplies_DoctorReviews");

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReviewReplies_Users");
        }
    }
}
