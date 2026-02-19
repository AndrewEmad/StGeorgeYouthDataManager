using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Infrastructure.Data.Configurations;

public class StudentEditLogConfiguration : IEntityTypeConfiguration<StudentEditLog>
{
    public void Configure(EntityTypeBuilder<StudentEditLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Details).HasMaxLength(2000);
        builder.Property(e => e.UpdatedByUserName).HasMaxLength(200);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => e.UpdatedAt);
        builder.HasOne(e => e.Student)
            .WithMany(s => s.EditLogs)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
