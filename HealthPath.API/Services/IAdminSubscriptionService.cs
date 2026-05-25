using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface IAdminSubscriptionService
{
    Task<ApiResponse<PageResponse<SubscriptionPlanDto>>> GetAllPlansAsync(int page = 1, int pageSize = 10);
    
    Task<ApiResponse<SubscriptionPlanDto>> GetPlanByIdAsync(Guid id);
    
    Task<ApiResponse<SubscriptionPlanDto>> CreatePlanAsync(CreateSubscriptionPlanDto planDto);
    
    Task<ApiResponse<SubscriptionPlanDto>> UpdatePlanAsync(Guid id, UpdateSubscriptionPlanDto planDto);
    
    Task<ApiResponse<bool>> DeletePlanAsync(Guid id);
    
    Task<ApiResponse<PageResponse<TransactionDto>>> GetTransactionsPagedAsync(string? search, string? platform, string? status, int page, int pageSize);
}
