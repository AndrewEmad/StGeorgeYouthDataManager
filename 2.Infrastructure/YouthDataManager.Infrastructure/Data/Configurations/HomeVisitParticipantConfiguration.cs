using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Infrastructure.Data.Configurations;

public class HomeVisitParticipantConfiguration : IEntityTypeConfiguration<HomeVisitParticipant>
{
    public void Configure(EntityTypeBuilder<HomeVisitParticipant> builder)
    {
        builder.HasKey(e => new { e.HomeVisitId, e.ServantId });

        builder.HasOne(e => e.HomeVisit)
            .WithMany(v => v.Participants)
            .HasForeignKey(e => e.HomeVisitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Servant)
            .WithMany(u => u.VisitParticipations)
            .HasForeignKey(e => e.ServantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
