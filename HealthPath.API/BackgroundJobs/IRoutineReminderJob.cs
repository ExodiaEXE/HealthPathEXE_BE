using System.Threading.Tasks;

namespace HealthPath.API.BackgroundJobs;

public interface IRoutineReminderJob
{
    Task ExecuteAsync();
}
