namespace HealthPath.API.Services;

/// <summary>
/// Abstraction over Hangfire vs inline execution when Hangfire is disabled (e.g. local dev).
/// </summary>
public interface IBackgroundJobDispatcher
{
    void EnqueueOtpEmail(
        string targetEmail,
        string targetName,
        string subject,
        string title,
        string bodyText);

    void ScheduleNotificationSend(Guid notificationId, TimeSpan delay);
}
