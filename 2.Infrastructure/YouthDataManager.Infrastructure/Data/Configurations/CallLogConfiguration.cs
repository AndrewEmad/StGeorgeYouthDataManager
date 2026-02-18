using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Infrastructure.Data.Configurations;

public class CallLogConfiguration : IEntityTypeConfiguration<CallLog>
{
    public void Configure(EntityTypeBuilder<CallLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(e => e.Student)
            .WithMany(s => s.CallLogs)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Servant)
            .WithMany(u => u.CallHistory)
            .HasForeignKey(e => e.ServantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.CallDate);
    }
}
