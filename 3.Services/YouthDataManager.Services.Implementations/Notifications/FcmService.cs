using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YouthDataManager.Notifications.Service.Abstractions;

namespace YouthDataManager.Notifications.Service.Implementations;

public sealed class FcmService(ILogger<FcmService> logger,IConfiguration configuration) : IFcmService,IPushTransport
{
    public async Task<PushResult> Send(string token,string appType,string title,string body,CancellationToken ct)
    {
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(int.TryParse(configuration["NotificationWorker:ProviderTimeoutSeconds"],out var seconds) ? seconds : 15,5,30)));
        try
        {
            var appUrl=configuration[$"Notifications:{appType}AppUrl"];
            if(!Uri.TryCreate(appUrl,UriKind.Absolute,out var uri) || uri.Scheme!="https")return new(false,false,"Notification application URL is not configured.");
            var root=uri.AbsoluteUri.TrimEnd('/')+"/";
            await FirebaseMessaging.DefaultInstance.SendAsync(new Message {
                Token=token,Notification=new(){Title=title,Body=body},
                Webpush=new(){FcmOptions=new(){Link=root+"#/dashboard"},Notification=new(){Title=title,Body=body,Icon=root+"logo.png"}},
                Data=new Dictionary<string,string>{{"url",root+"#/dashboard"}}
            },deadline.Token);
            return new(true,false,null);
        }
        catch(OperationCanceledException) when(ct.IsCancellationRequested){throw;}
        catch(OperationCanceledException){logger.LogWarning("Notification provider timed out");return new(false,false,"Provider timeout");}
        catch(FirebaseMessagingException ex)
        {
            logger.LogWarning("Notification provider failure: {Code}",ex.MessagingErrorCode);
            return new(false,ex.MessagingErrorCode==MessagingErrorCode.Unregistered || ex.MessagingErrorCode==MessagingErrorCode.SenderIdMismatch,ex.MessagingErrorCode?.ToString() ?? "Provider failure");
        }
        catch(Exception ex){logger.LogError(ex,"Notification provider unavailable");return new(false,false,"Provider unavailable");}
    }
    public async Task<bool> SendToDeviceAsync(string token,string title,string body) => (await Send(token,"Servant",title,body,CancellationToken.None)).Success;
    public async Task<int> SendToMultipleAsync(IEnumerable<string> tokens,string title,string body)
    {
        var count=0;
        await Parallel.ForEachAsync(tokens.Distinct(),new ParallelOptions {MaxDegreeOfParallelism=4},async(token,ct)=>{if((await Send(token,"Servant",title,body,ct)).Success)Interlocked.Increment(ref count);});
        return count;
    }
}
