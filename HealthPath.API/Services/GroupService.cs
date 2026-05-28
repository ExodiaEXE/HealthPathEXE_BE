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

            var result = new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CreatedAt = group.CreatedAt,
                MemberCount = 1
            };

            return ApiResponse<GroupDto>.Ok(result, "Tạo nhóm mới thành công! Bạn đã trở thành chủ nhóm.");
        }

        public async Task<ApiResponse<List<GroupDto>>> GetMyGroupsAsync(Guid userId)
        {
            var groups = await _context.GroupMembers
                .Where(m => m.UserId == userId && m.DeletedAt == null)
                .Include(m => m.Group)
                .Where(m => m.Group.DeletedAt == null)
                .Select(m => new GroupDto
                {
                    Id = m.Group.Id,
                    Name = m.Group.Name,
                    Description = m.Group.Description,
                    CreatedAt = m.Group.CreatedAt,
                    MemberCount = _context.GroupMembers.Count(gm => gm.GroupId == m.GroupId && gm.DeletedAt == null)
                })
                .ToListAsync();

            return ApiResponse<List<GroupDto>>.Ok(groups, "Lấy danh sách nhóm của bạn thành công.");
        }

        public async Task<ApiResponse<GroupDto>> GetByIdAsync(Guid id, Guid userId)
        {
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id && g.DeletedAt == null);

            if (group == null)
            {
                return ApiResponse<GroupDto>.Fail("Không tìm thấy nhóm này hoặc nhóm đã bị giải tán.", "GROUP_NOT_FOUND");
            }

            var dto = new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CreatedAt = group.CreatedAt,
                MemberCount = await _context.GroupMembers.CountAsync(gm => gm.GroupId == id && gm.DeletedAt == null)
            };

            return ApiResponse<GroupDto>.Ok(dto, "Lấy chi tiết nhóm thành công.");
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

            var result = new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CreatedAt = group.CreatedAt,
                MemberCount = await _context.GroupMembers.CountAsync(gm => gm.GroupId == id && gm.DeletedAt == null)
            };

            return ApiResponse<GroupDto>.Ok(result, "Cập nhật thông tin nhóm thành công.");
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

            var alreadyMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == id && m.UserId == userId && m.DeletedAt == null);

            if (alreadyMember)
            {
                return ApiResponse<object>.Fail("Bạn đã là thành viên của nhóm này rồi.", "ALREADY_MEMBER");
            }

            // SỬA TẠI ĐÂY: Loại bỏ hoàn toàn CreatedAt và UpdatedAt tại luồng tham gia nhóm
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
    }
}