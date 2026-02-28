using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Infrastructure.Data.Configurations;

public class NotificationScheduleConfiguration : IEntityTypeConfiguration<NotificationSchedule>
{
    public void Configure(EntityTypeBuilder<NotificationSchedule> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DaysOfWeek).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TimeOfDay).IsRequired();

        builder.HasOne(e => e.User)
            .WithMany(u => u.NotificationSchedules)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);
    }
}
