using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Infrastructure.Data.Configurations;

public class ServantAttendanceConfiguration : IEntityTypeConfiguration<ServantAttendance>
{
    public void Configure(EntityTypeBuilder<ServantAttendance> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.ServantId, e.Date }).IsUnique();
        builder.HasOne(e => e.Servant)
            .WithMany(u => u.AttendanceRecords)
            .HasForeignKey(e => e.ServantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
