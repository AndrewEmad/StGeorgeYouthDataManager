namespace YouthDataManager.Notifications.Service.Abstractions;
public record PushResult(bool Success,bool InvalidToken,string? Error);
public interface IPushTransport
{
    Task<PushResult> Send(string token,string appType,string title,string body,CancellationToken ct);
}
