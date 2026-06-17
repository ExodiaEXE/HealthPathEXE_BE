using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthPath.API.Services
{
    public class GroupService : IGroupService
    {
        private readonly HealthpathDbContext _context;

        public GroupService(HealthpathDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<GroupDto>> CreateGroupAsync(Guid userId, CreateGroupDto dto)
        {
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                InviteCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(), // Sinh mã invitecode ngắn gọn
                OwnerId = userId,
                MaxMembers = 50, // Mặc định giới hạn nhóm
                IsPublic = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // SỬA TẠI ĐÂY: Loại bỏ hoàn toàn CreatedAt và UpdatedAt không có trong Model GroupMember
            var member = new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                UserId = userId,
                Role = "Owner", // Gán vai trò chủ nhóm
                JoinedAt = DateTime.UtcNow,
                DeletedAt = null
            };

            _context.GroupMembers.Add(member);
            await _context.SaveChangesAsync();

            return ApiResponse<GroupDto>.Ok(
                await MapGroupDtoAsync(group.Id),
                "Tạo nhóm mới thành công! Bạn đã trở thành chủ nhóm.");
        }

        public async Task<ApiResponse<List<GroupDto>>> GetMyGroupsAsync(Guid userId)
        {
            var groups = await _context.GroupMembers
                .Where(m => m.UserId == userId && m.DeletedAt == null)
                .Include(m => m.Group)
                .Where(m => m.Group.DeletedAt == null)
                .Select(m => m.Group)
                .ToListAsync();

            var result = new List<GroupDto>();
            foreach (var group in groups)
            {
                result.Add(await MapGroupDtoAsync(group.Id));
            }

            return ApiResponse<List<GroupDto>>.Ok(result, "Lấy danh sách nhóm của bạn thành công.");
        }

        public async Task<ApiResponse<GroupDto>> GetByIdAsync(Guid id, Guid userId)
        {
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

            if (group == null)
            {
                return ApiResponse<GroupDto>.Fail("Không tìm thấy nhóm này hoặc nhóm đã bị giải tán.", "GROUP_NOT_FOUND");
            }

            return ApiResponse<GroupDto>.Ok(
                await MapGroupDtoAsync(group.Id),
                "Lấy chi tiết nhóm thành công.");
        }

        public async Task<ApiResponse<GroupDto>> UpdateGroupAsync(Guid id, Guid userId, UpdateGroupDto dto)
        {
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

            if (group == null)
            {
                return ApiResponse<GroupDto>.Fail("Nhóm không tồn tại.", "GROUP_NOT_FOUND");
            }

            group.Name = dto.Name;
            group.Description = dto.Description;
            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<GroupDto>.Ok(
                await MapGroupDtoAsync(group.Id),
                "Cập nhật thông tin nhóm thành công.");
        }

        public async Task<ApiResponse<object>> DeleteGroupAsync(Guid id, Guid userId)
        {
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

            if (group == null)
            {
                return ApiResponse<object>.Fail("Nhóm không tồn tại hoặc đã bị giải tán trước đó.", "GROUP_NOT_FOUND");
            }

            group.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<object>.Ok(new { }, "Giải tán nhóm thành công.");
        }

        public async Task<ApiResponse<object>> JoinGroupAsync(Guid id, Guid userId)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == id && g.DeletedAt == null);
            if (!groupExists)
            {
                return ApiResponse<object>.Fail("Nhóm muốn tham gia không tồn tại hoặc đã bị giải tán.", "GROUP_NOT_FOUND");
            }

            var existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == userId);

            if (existingMember != null)
            {
                if (existingMember.DeletedAt == null)
                {
                    return ApiResponse<object>.Fail(
                        "Bạn đã là thành viên của nhóm này rồi.",
                        "ALREADY_MEMBER");
                }

                // Đã từng rời nhóm — khôi phục bản ghi cũ (tránh trùng unique group_id + user_id).
                existingMember.DeletedAt = null;
                existingMember.JoinedAt = DateTime.UtcNow;
                existingMember.Role = "Member";
                await _context.SaveChangesAsync();

                return ApiResponse<object>.Ok(new { }, "Tham gia vào nhóm thành công!");
            }

            var member = new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = id,
                UserId = userId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow,
                DeletedAt = null
            };

            _context.GroupMembers.Add(member);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.Ok(new { }, "Tham gia vào nhóm thành công!");
        }

        public async Task<ApiResponse<List<GroupDto>>> GetPublicGroupsAsync(Guid userId, string? search)
        {
            var myGroupIds = await _context.GroupMembers
                .Where(m => m.UserId == userId && m.DeletedAt == null)
                .Select(m => m.GroupId)
                .ToListAsync();

            var query = _context.Groups
                .Where(g => g.DeletedAt == null && g.IsPublic && !myGroupIds.Contains(g.Id));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(g => g.Name.ToLower().Contains(term));
            }

            var groups = await query
                .OrderByDescending(g => g.CreatedAt)
                .Take(50)
                .ToListAsync();

            var result = new List<GroupDto>();
            foreach (var group in groups)
            {
                result.Add(await MapGroupDtoAsync(group.Id));
            }

            return ApiResponse<List<GroupDto>>.Ok(result, "Lấy danh sách nhóm phổ biến thành công.");
        }

        public async Task<ApiResponse<List<GroupMemberDto>>> GetGroupMembersAsync(Guid groupId, Guid userId)
        {
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == groupId && g.DeletedAt == null);
            if (!groupExists)
            {
                return ApiResponse<List<GroupMemberDto>>.Fail(
                    "Nhóm không tồn tại hoặc đã bị giải tán.",
                    "GROUP_NOT_FOUND");
            }

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == userId && m.DeletedAt == null);
            if (!isMember)
            {
                return ApiResponse<List<GroupMemberDto>>.Fail(
                    "Bạn chưa là thành viên của nhóm này.",
                    "FORBIDDEN");
            }

            var members = await _context.GroupMembers
                .Where(m => m.GroupId == groupId && m.DeletedAt == null)
                .Include(m => m.User)
                .OrderByDescending(m => m.Role == "Owner")
                .ThenBy(m => m.JoinedAt)
                .ToListAsync();

            var result = new List<GroupMemberDto>();
            foreach (var member in members)
            {
                var weeklyPoints = await CalculateWeeklyLeaderboardPointsAsync(
                    member.UserId,
                    groupId);

                result.Add(new GroupMemberDto
                {
                    UserId = member.UserId,
                    Name = member.User.FullName,
                    Role = member.Role,
                    IsCurrentUser = member.UserId == userId,
                    WeeklyScore = weeklyPoints,
                });
            }

            return ApiResponse<List<GroupMemberDto>>.Ok(result, "Lấy danh sách thành viên thành công.");
        }

        public async Task<ApiResponse<object>> CheckInGroupAsync(Guid groupId, Guid userId)
        {
            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == userId && m.DeletedAt == null);
            if (!isMember)
            {
                return ApiResponse<object>.Fail(
                    "Bạn không phải thành viên của nhóm này.",
                    "NOT_MEMBER");
            }

            var dayStart = DateTime.UtcNow.Date;
            var dayEnd = dayStart.AddDays(1);
            var exists = await _context.GroupTeamCheckins.AnyAsync(c =>
                c.GroupId == groupId &&
                c.UserId == userId &&
                c.DeletedAt == null &&
                c.CheckinDate >= dayStart &&
                c.CheckinDate < dayEnd);
            if (exists)
            {
                return ApiResponse<object>.Ok(new { }, "Điểm danh nhóm thành công.");
            }

            try
            {
                _context.GroupTeamCheckins.Add(new GroupTeamCheckin
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = userId,
                    CheckinDate = DateTime.SpecifyKind(dayStart, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow,
                });
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // Double tap / race: row already inserted for today.
            }

            return ApiResponse<object>.Ok(new { }, "Điểm danh nhóm thành công.");
        }

        public async Task<ApiResponse<LeaveGroupResultDto>> LeaveGroupAsync(Guid groupId, Guid userId)
        {
            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId && m.DeletedAt == null);

            if (member == null)
            {
                return ApiResponse<LeaveGroupResultDto>.Fail(
                    "Bạn không phải thành viên của nhóm này.",
                    "NOT_MEMBER");
            }

            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId && g.DeletedAt == null);

            if (group == null)
            {
                return ApiResponse<LeaveGroupResultDto>.Fail(
                    "Nhóm không tồn tại hoặc đã bị giải tán.",
                    "GROUP_NOT_FOUND");
            }

            member.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var remainingCount = await _context.GroupMembers
                .CountAsync(m => m.GroupId == groupId && m.DeletedAt == null);

            var groupDeleted = false;
            if (remainingCount == 0)
            {
                group.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                groupDeleted = true;
            }

            var message = groupDeleted
                ? "Bạn đã rời nhóm. Nhóm đã được giải tán vì không còn thành viên."
                : "Rời nhóm thành công.";

            return ApiResponse<LeaveGroupResultDto>.Ok(
                new LeaveGroupResultDto { GroupDeleted = groupDeleted },
                message);
        }

        public async Task<ApiResponse<GroupDto>> JoinGroupByInviteCodeAsync(Guid userId, JoinGroupByInviteCodeDto dto)
        {
            var code = dto.InviteCode?.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(code))
            {
                return ApiResponse<GroupDto>.Fail("Mã mời không hợp lệ.", "VALIDATION_ERROR");
            }

            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.InviteCode == code && g.DeletedAt == null);
            if (group == null)
            {
                return ApiResponse<GroupDto>.Fail("Không tìm thấy nhóm với mã mời này.", "GROUP_NOT_FOUND");
            }

            var join = await JoinGroupAsync(group.Id, userId);
            if (!join.Success)
            {
                return ApiResponse<GroupDto>.Fail(join.Message ?? "Không thể tham gia nhóm.", join.ErrorCode ?? "INTERNAL_ERROR");
            }

            return ApiResponse<GroupDto>.Ok(
                await MapGroupDtoAsync(group.Id),
                "Tham gia nhóm bằng mã mời thành công!");
        }

        private static DateTime GetCurrentWeekMondayUtc()
        {
            var today = DateTime.UtcNow.Date;
            var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return today.AddDays(-diff);
        }

        private async Task<int> CalculateWeeklyLeaderboardPointsAsync(Guid userId, Guid groupId)
        {
            const double halfDayWeight = 100.0 / 14.0;
            var weekStart = GetCurrentWeekMondayUtc();
            double sum = 0;

            for (var i = 0; i < 7; i++)
            {
                var dayStart = weekStart.AddDays(i);
                var dayEnd = dayStart.AddDays(1);

                var hasCheckIn = await _context.GroupTeamCheckins.AnyAsync(c =>
                    c.GroupId == groupId &&
                    c.UserId == userId &&
                    c.DeletedAt == null &&
                    c.CheckinDate >= dayStart &&
                    c.CheckinDate < dayEnd);

                if (hasCheckIn)
                {
                    sum += halfDayWeight;
                }

                var completedOnDay = await _context.UserRoutines.CountAsync(ur =>
                    ur.UserId == userId &&
                    ur.Status == "completed" &&
                    ur.CompletedAt >= dayStart &&
                    ur.CompletedAt < dayEnd &&
                    ur.DeletedAt == null);

                if (completedOnDay <= 0)
                {
                    continue;
                }

                var scheduledOnDay = await _context.UserRoutines.CountAsync(ur =>
                    ur.UserId == userId &&
                    ur.ScheduledAt >= dayStart &&
                    ur.ScheduledAt < dayEnd &&
                    ur.DeletedAt == null &&
                    ur.Status != "cancelled");

                var total = Math.Max(scheduledOnDay, completedOnDay);
                sum += (completedOnDay / (double)total) * halfDayWeight;
            }

            return (int)Math.Round(Math.Clamp(sum, 0.0, 100.0), MidpointRounding.AwayFromZero);
        }

        private async Task<GroupDto> MapGroupDtoAsync(Guid groupId)
        {
            var group = await _context.Groups.FirstAsync(g => g.Id == groupId);
            return new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                InviteCode = group.InviteCode,
                CreatedAt = group.CreatedAt,
                MemberCount = await _context.GroupMembers.CountAsync(
                    gm => gm.GroupId == groupId && gm.DeletedAt == null),
            };
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            return message.Contains("23505", StringComparison.Ordinal)
                   || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
        }
    }
}