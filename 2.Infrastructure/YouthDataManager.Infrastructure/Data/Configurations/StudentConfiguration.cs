using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Infrastructure.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Phone).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Area).HasMaxLength(100);
        builder.Property(e => e.College).HasMaxLength(100);

        builder.HasIndex(e => e.Phone);
        builder.HasIndex(e => e.Area);
        builder.HasIndex(e => e.FullName);

        builder.HasOne(e => e.Servant)
            .WithMany(u => u.Students)
            .HasForeignKey(e => e.ServantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
