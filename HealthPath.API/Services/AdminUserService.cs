using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthPath.API.Services;

public class AdminUserService : IAdminUserService
{
    private readonly HealthpathDbContext _context;

    public AdminUserService(HealthpathDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PageResponse<AdminUserSummaryDto>>> GetUsersPagedAsync(string? search, bool? onlyPremium, int page, int pageSize)
    {
        var query = _context.Users
            .Where(u => u.DeletedAt == null);

        // Apply Search Filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(cleanSearch) || u.Email.ToLower().Contains(cleanSearch));
        }

        // Apply Premium Filter
        if (onlyPremium.HasValue && onlyPremium.Value)
        {
            var activeUserIds = _context.UserSubscriptions
                .Where(s => s.Status == "active" && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow) && s.DeletedAt == null)
                .Select(s => s.UserId);

            query = query.Where(u => activeUserIds.Contains(u.Id));
        }

        // Count Total Items
        long totalItems = await query.CountAsync();

        // Paginate and get users
        var dbUsers = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map to Summary DTOs
        var activeSubMap = await _context.UserSubscriptions
            .Where(s => s.Status == "active" && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow) && s.DeletedAt == null)
            .ToDictionaryAsync(s => s.UserId, s => s);

        var summaryItems = dbUsers.Select(u => new AdminUserSummaryDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            IsActive = u.IsActive,
            IsVerified = u.IsVerified,
            HasPremiumAccess = activeSubMap.ContainsKey(u.Id),
            CreatedAt = u.CreatedAt
        }).ToList();

        var pageResponse = new PageResponse<AdminUserSummaryDto>(summaryItems, totalItems, page, pageSize);

        return ApiResponse<PageResponse<AdminUserSummaryDto>>.Ok(pageResponse, "Lấy danh sách người dùng thành công.");
    }

    public async Task<ApiResponse<AdminUserDetailDto>> GetUserDetailAsync(Guid id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

        if (user == null)
        {
            return ApiResponse<AdminUserDetailDto>.Fail("Không tìm thấy người dùng.", ErrorCode.INVALID_CREDENTIALS);
        }

        // Fetch active subscription if any
        var sub = await _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == id && s.DeletedAt == null)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync();

        UserSubscriptionDto? activeSubDto = null;
        if (sub != null)
        {
            bool isActive = sub.Status == "active" && (sub.ExpiresAt == null || sub.ExpiresAt > DateTime.UtcNow);
            activeSubDto = new UserSubscriptionDto
            {
                Id = sub.Id,
                UserId = sub.UserId,
                PlanId = sub.PlanId,
                PlanName = sub.Plan.Name,
                Status = sub.Status,
                BillingCycle = sub.BillingCycle,
                StartedAt = sub.StartedAt,
                ExpiresAt = sub.ExpiresAt,
                CancelledAt = sub.CancelledAt,
                PaymentProvider = sub.PaymentProvider,
                PaymentRef = sub.PaymentRef,
                IsActiveSubscription = isActive
            };
        }

        var detailDto = new AdminUserDetailDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            IsVerified = user.IsVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            ActiveSubscription = activeSubDto
        };

        return ApiResponse<AdminUserDetailDto>.Ok(detailDto, "Lấy chi tiết người dùng thành công.");
    }

    public async Task<ApiResponse<AdminUserSummaryDto>> CreateUserAsync(AdminCreateUserDto request)
    {
        // 1. Check duplicate email
        if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.DeletedAt == null))
        {
            return ApiResponse<AdminUserSummaryDto>.Fail("Email đã được sử dụng trên hệ thống.", ErrorCode.EMAIL_TAKEN);
        }

        // 2. Hash Password
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 3. Create user
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHash,
            Phone = request.Phone,
            IsActive = true,
            IsVerified = true,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        var summaryDto = new AdminUserSummaryDto
        {
            Id = newUser.Id,
            FullName = newUser.FullName,
            Email = newUser.Email,
            Phone = newUser.Phone,
            IsActive = newUser.IsActive,
            IsVerified = newUser.IsVerified,
            HasPremiumAccess = false,
            CreatedAt = newUser.CreatedAt
        };

        return ApiResponse<AdminUserSummaryDto>.Ok(summaryDto, "Admin tạo mới tài khoản người dùng thành công!");
    }

    public async Task<ApiResponse<bool>> ToggleUserActiveAsync(Guid id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);

        if (user == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy người dùng.", ErrorCode.INVALID_CREDENTIALS);
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        string action = user.IsActive ? "Mở khóa" : "Khóa";
        return ApiResponse<bool>.Ok(user.IsActive, $"{action} tài khoản người dùng thành công.");
    }
}
