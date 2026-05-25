using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface ISubscriptionService
{
    Task<ApiResponse<List<SubscriptionPlanDto>>> GetPlansAsync();
    
    Task<ApiResponse<UserSubscriptionDto?>> GetCurrentSubscriptionAsync(Guid userId);
    
    Task<ApiResponse<List<TransactionDto>>> GetMyTransactionsAsync(Guid userId);
    
    Task<ApiResponse<UserSubscriptionDto>> VerifyAndFulfillPurchaseAsync(Guid userId, VerifyReceiptRequestDto request);
    
    Task<ApiResponse<bool>> ProcessServerNotificationAsync(string platform, JsonElement payload);
}
