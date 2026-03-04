namespace YouthDataManager.Domain.Entities;

public class HomeVisitParticipant
{
    public Guid HomeVisitId { get; set; }
    public HomeVisit HomeVisit { get; set; } = null!;
    public Guid ServantId { get; set; }
    public ApplicationUser Servant { get; set; } = null!;
}
