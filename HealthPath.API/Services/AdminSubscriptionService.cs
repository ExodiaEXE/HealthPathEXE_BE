using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthPath.API.Services;

public class AdminSubscriptionService : IAdminSubscriptionService
{
    private readonly HealthpathDbContext _context;

    public AdminSubscriptionService(HealthpathDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PageResponse<SubscriptionPlanDto>>> GetAllPlansAsync(int page = 1, int pageSize = 10)
    {
        var query = _context.SubscriptionPlans
            .Where(p => p.DeletedAt == null);

        long totalItems = await query.CountAsync();

        var plans = await query
            .OrderBy(p => p.PriceMonthly)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                Description = p.Description,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                Currency = p.Currency,
                Features = p.Features,
                IsActive = p.IsActive,
                AppleProductId = p.AppleProductId,
                GoogleProductId = p.GoogleProductId
            })
            .ToListAsync();

        var pageResponse = new PageResponse<SubscriptionPlanDto>(plans, totalItems, page, pageSize);
        return ApiResponse<PageResponse<SubscriptionPlanDto>>.Ok(pageResponse, "Lấy danh sách các gói thành công.");
    }

    public async Task<ApiResponse<SubscriptionPlanDto>> GetPlanByIdAsync(Guid id)
    {
        var p = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (p == null)
        {
            return ApiResponse<SubscriptionPlanDto>.Fail("Không tìm thấy gói subscription.", ErrorCode.SUBSCRIPTION_PLAN_NOT_FOUND);
        }

        var dto = new SubscriptionPlanDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Description = p.Description,
            PriceMonthly = p.PriceMonthly,
            PriceYearly = p.PriceYearly,
            Currency = p.Currency,
            Features = p.Features,
            IsActive = p.IsActive,
            AppleProductId = p.AppleProductId,
            GoogleProductId = p.GoogleProductId
        };

        return ApiResponse<SubscriptionPlanDto>.Ok(dto, "Lấy chi tiết gói thành công.");
    }

    public async Task<ApiResponse<SubscriptionPlanDto>> CreatePlanAsync(CreateSubscriptionPlanDto planDto)
    {
        // Check duplicate code
        if (await _context.SubscriptionPlans.AnyAsync(p => p.Code == planDto.Code && p.DeletedAt == null))
        {
            return ApiResponse<SubscriptionPlanDto>.Fail("Mã Code của gói đã tồn tại trên hệ thống.", ErrorCode.EMAIL_TAKEN);
        }

        var newPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = planDto.Name,
            Code = planDto.Code,
            Description = planDto.Description,
            PriceMonthly = planDto.PriceMonthly,
            PriceYearly = planDto.PriceYearly,
            Currency = planDto.Currency ?? "VND",
            Features = planDto.Features ?? "[]",
            IsActive = planDto.IsActive,
            AppleProductId = planDto.AppleProductId,
            GoogleProductId = planDto.GoogleProductId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SubscriptionPlans.Add(newPlan);
        await _context.SaveChangesAsync();

        var dto = new SubscriptionPlanDto
        {
            Id = newPlan.Id,
            Name = newPlan.Name,
            Code = newPlan.Code,
            Description = newPlan.Description,
            PriceMonthly = newPlan.PriceMonthly,
            PriceYearly = newPlan.PriceYearly,
            Currency = newPlan.Currency,
            Features = newPlan.Features,
            IsActive = newPlan.IsActive,
            AppleProductId = newPlan.AppleProductId,
            GoogleProductId = newPlan.GoogleProductId
        };

        return ApiResponse<SubscriptionPlanDto>.Ok(dto, "Tạo gói subscription mới thành công!");
    }

    public async Task<ApiResponse<SubscriptionPlanDto>> UpdatePlanAsync(Guid id, UpdateSubscriptionPlanDto planDto)
    {
        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (plan == null)
        {
            return ApiResponse<SubscriptionPlanDto>.Fail("Không tìm thấy gói subscription cần cập nhật.", ErrorCode.SUBSCRIPTION_PLAN_NOT_FOUND);
        }

        // Update properties
        plan.Name = planDto.Name;
        plan.Description = planDto.Description;
        plan.PriceMonthly = planDto.PriceMonthly;
        plan.PriceYearly = planDto.PriceYearly;
        plan.Currency = planDto.Currency ?? "VND";
        plan.Features = planDto.Features ?? "[]";
        plan.IsActive = planDto.IsActive;
        plan.AppleProductId = planDto.AppleProductId;
        plan.GoogleProductId = planDto.GoogleProductId;
        plan.UpdatedAt = DateTime.UtcNow;

        _context.SubscriptionPlans.Update(plan);
        await _context.SaveChangesAsync();

        var dto = new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Code = plan.Code,
            Description = plan.Description,
            PriceMonthly = plan.PriceMonthly,
            PriceYearly = plan.PriceYearly,
            Currency = plan.Currency,
            Features = plan.Features,
            IsActive = plan.IsActive,
            AppleProductId = plan.AppleProductId,
            GoogleProductId = plan.GoogleProductId
        };

        return ApiResponse<SubscriptionPlanDto>.Ok(dto, "Cập nhật gói subscription thành công!");
    }

    public async Task<ApiResponse<bool>> DeletePlanAsync(Guid id)
    {
        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);

        if (plan == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy gói subscription cần xóa.", ErrorCode.SUBSCRIPTION_PLAN_NOT_FOUND);
        }

        // Soft delete
        plan.DeletedAt = DateTime.UtcNow;
        _context.SubscriptionPlans.Update(plan);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Xóa gói subscription thành công (Soft delete).");
    }

    public async Task<ApiResponse<PageResponse<TransactionDto>>> GetTransactionsPagedAsync(string? search, string? platform, string? status, int page, int pageSize)
    {
        var query = _context.Transactions
            .Include(t => t.Plan)
            .Include(t => t.User)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(t => t.PlatformTransactionId.ToLower().Contains(cleanSearch) || 
                                     t.User.Email.ToLower().Contains(cleanSearch) ||
                                     t.User.FullName.ToLower().Contains(cleanSearch));
        }

        if (!string.IsNullOrWhiteSpace(platform))
        {
            query = query.Where(t => t.Platform == platform);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        long totalItems = await query.CountAsync();

        var dbTransactions = await query
            .OrderByDescending(t => t.PurchasedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = dbTransactions.Select(t => new TransactionDto
        {
            Id = t.Id,
            UserId = t.UserId,
            PlanId = t.PlanId,
            PlanName = t.Plan.Name,
            Platform = t.Platform,
            PlatformTransactionId = t.PlatformTransactionId,
            OriginalTransactionId = t.OriginalTransactionId,
            Status = t.Status,
            Amount = t.Amount,
            Currency = t.Currency,
            PurchasedAt = t.PurchasedAt,
            ExpiresAt = t.ExpiresAt,
            CreatedAt = t.CreatedAt
        }).ToList();

        var pageResponse = new PageResponse<TransactionDto>(dtos, totalItems, page, pageSize);
        return ApiResponse<PageResponse<TransactionDto>>.Ok(pageResponse, "Lấy danh sách giao dịch thành công.");
    }
}
