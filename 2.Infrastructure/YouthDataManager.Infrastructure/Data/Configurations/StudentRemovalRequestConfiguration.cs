using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;

namespace YouthDataManager.Infrastructure.Data.Configurations;

public class StudentRemovalRequestConfiguration : IEntityTypeConfiguration<StudentRemovalRequest>
{
    public void Configure(EntityTypeBuilder<StudentRemovalRequest> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RequestedByUser)
            .WithMany()
            .HasForeignKey(e => e.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProcessedByUser)
            .WithMany()
            .HasForeignKey(e => e.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.StudentId, e.RequestedByUserId });
        builder.HasIndex(e => e.Status);
    }
}
