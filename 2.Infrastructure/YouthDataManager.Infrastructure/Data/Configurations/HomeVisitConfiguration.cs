using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Infrastructure.Data.Configurations;

public class HomeVisitConfiguration : IEntityTypeConfiguration<HomeVisit>
{
    public void Configure(EntityTypeBuilder<HomeVisit> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(e => e.Student)
            .WithMany(s => s.HomeVisits)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Servant)
            .WithMany(u => u.VisitHistory)
            .HasForeignKey(e => e.ServantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.VisitDate);
    }
}
