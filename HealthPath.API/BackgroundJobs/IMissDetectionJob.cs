using System.Threading.Tasks;

namespace HealthPath.API.BackgroundJobs;

public interface IMissDetectionJob
{
    Task ExecuteAsync();
}
