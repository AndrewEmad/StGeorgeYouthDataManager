using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;

namespace YouthDataManager.Infrastructure.Data.Configurations;

public class StudentAdditionRequestConfiguration : IEntityTypeConfiguration<StudentAdditionRequest>
{
    public void Configure(EntityTypeBuilder<StudentAdditionRequest> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.FullName).HasMaxLength(200);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.SecondaryPhone).HasMaxLength(50);
        builder.Property(e => e.Area).HasMaxLength(200);
        builder.Property(e => e.College).HasMaxLength(200);
        builder.Property(e => e.AcademicYear).HasMaxLength(100);

        builder.HasOne(e => e.RequestedByUser)
            .WithMany()
            .HasForeignKey(e => e.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProcessedByUser)
            .WithMany()
            .HasForeignKey(e => e.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.RequestedByUserId);
    }
}
