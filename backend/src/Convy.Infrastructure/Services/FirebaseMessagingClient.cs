using FirebaseAdmin.Messaging;

namespace Convy.Infrastructure.Services;

public interface IFirebaseMessagingClient
{
    Task<FirebaseMulticastSendResult> SendMulticastAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken = default);
}

public record FirebaseMulticastSendResult(int SuccessCount, IReadOnlyList<FirebaseSendFailure> Failures);

public record FirebaseSendFailure(string Token, string ErrorCode, string? Message);

public class FirebaseMessagingClient : IFirebaseMessagingClient
{
    public async Task<FirebaseMulticastSendResult> SendMulticastAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken = default)
    {
        var message = new MulticastMessage
        {
            Tokens = tokens.ToList(),
            Notification = new Notification
            {
                Title = title,
                Body = body,
            },
            Android = new AndroidConfig
            {
                Notification = new AndroidNotification
                {
                    ChannelId = "list_updates",
                },
            },
            Data = data is null ? null : new Dictionary<string, string>(data),
        };

        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);
        var failures = response.Responses
            .Select((sendResponse, index) => new { sendResponse, token = tokens[index] })
            .Where(entry => !entry.sendResponse.IsSuccess)
            .Select(entry => new FirebaseSendFailure(
                entry.token,
                GetErrorCode(entry.sendResponse.Exception),
                entry.sendResponse.Exception?.Message))
            .ToList();

        return new FirebaseMulticastSendResult(response.SuccessCount, failures);
    }

    private static string GetErrorCode(Exception? exception) =>
        exception is FirebaseMessagingException messagingException
            ? messagingException.MessagingErrorCode.ToString() ?? "Unknown"
            : exception?.GetType().Name ?? "Unknown";
}
