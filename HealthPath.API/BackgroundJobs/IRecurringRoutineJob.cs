using System.Threading.Tasks;

namespace HealthPath.API.BackgroundJobs;

public interface IRecurringRoutineJob
{
    Task ExecuteAsync();
}
