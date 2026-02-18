using System.Threading.Tasks;

namespace YouthDataManager.Shared.Service.Abstractions
{
    public interface IActivityLogger
    {
        Task LogAsync(string action, string details);
    }
}
