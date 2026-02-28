namespace YouthDataManager.Notifications.Service.Abstractions;

public interface IFcmService
{
    Task<bool> SendToDeviceAsync(string token, string title, string body);
    Task<int> SendToMultipleAsync(IEnumerable<string> tokens, string title, string body);
}
