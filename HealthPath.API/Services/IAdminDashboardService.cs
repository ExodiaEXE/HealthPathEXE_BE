using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface IAdminDashboardService
{
    Task<ApiResponse<DashboardDto>> GetDashboardStatsAsync();
}
