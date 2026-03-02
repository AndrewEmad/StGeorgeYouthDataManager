using System.Threading;
using System.Threading.Tasks;

namespace YouthDataManager.Domain.Events;

public interface IDomainEventDispatcher
{
    Task Dispatch<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
}
