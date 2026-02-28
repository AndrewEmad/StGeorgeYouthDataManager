using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using YouthDataManager.Notifications.Service.Abstractions;

namespace YouthDataManager.Notifications.Service.Implementations;

public class FcmService : IFcmService
{
    private readonly ILogger<FcmService> _logger;

    public FcmService(ILogger<FcmService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendToDeviceAsync(string token, string title, string body)
    {
        try
        {
            var message = new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = title,
                        Body = body,
                        Icon = "/icons/icon-192x192.png"
                    }
                }
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("FCM message sent successfully: {Response}", response);
            return true;
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning(ex, "Failed to send FCM message to token {Token}", token);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending FCM message to token {Token}", token);
            return false;
        }
    }

    public async Task<int> SendToMultipleAsync(IEnumerable<string> tokens, string title, string body)
    {
        var tokenList = tokens.ToList();
        if (tokenList.Count == 0) return 0;

        var successCount = 0;
        foreach (var token in tokenList)
        {
            if (await SendToDeviceAsync(token, title, body))
                successCount++;
        }

        return successCount;
    }
}
