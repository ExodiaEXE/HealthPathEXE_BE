using System.Threading.Tasks;
using HealthPath.API.Models;

namespace HealthPath.API.Services.Channels;

public interface INotificationChannel
{
    string Name { get; }
    Task SendAsync(Notification notification, User user);
}
