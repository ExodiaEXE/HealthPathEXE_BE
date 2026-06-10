using Hangfire;

namespace HealthPath.API.Services;

public class HangfireBackgroundJobDispatcher : IBackgroundJobDispatcher
{
    private readonly IBackgroundJobClient _backgroundJobs;

    public HangfireBackgroundJobDispatcher(IBackgroundJobClient backgroundJobs)
    {
        _backgroundJobs = backgroundJobs;
    }

    public void EnqueueOtpEmail(
        string targetEmail,
        string targetName,
        string subject,
        string title,
        string bodyText)
    {
        _backgroundJobs.Enqueue<AuthService>(service =>
            service.SendOtpEmailAsync(targetEmail, targetName, subject, title, bodyText));
    }

    public void ScheduleNotificationSend(Guid notificationId, TimeSpan delay)
    {
        _backgroundJobs.Schedule<NotificationService>(
            service => service.SendDirectAsync(notificationId),
            delay);
    }
}
