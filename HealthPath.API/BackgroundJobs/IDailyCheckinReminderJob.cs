using System.Threading.Tasks;

namespace HealthPath.API.BackgroundJobs;

public interface IDailyCheckinReminderJob
{
    Task ExecuteAsync();
}
