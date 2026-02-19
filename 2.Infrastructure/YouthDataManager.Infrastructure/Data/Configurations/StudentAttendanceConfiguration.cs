using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Infrastructure.Data.Configurations;

public class StudentAttendanceConfiguration : IEntityTypeConfiguration<StudentAttendance>
{
    public void Configure(EntityTypeBuilder<StudentAttendance> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.StudentId, e.Date }).IsUnique();
        builder.HasOne(e => e.Student)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
