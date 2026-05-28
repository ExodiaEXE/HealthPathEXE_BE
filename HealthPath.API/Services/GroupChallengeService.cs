using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthPath.API.Services
{
    public class GroupChallengeService : IGroupChallengeService
    {
        private readonly HealthpathDbContext _context;

        public GroupChallengeService(HealthpathDbContext context)
        {
            _context = context;
        }

        public async Task<GroupChallenge> CreateChallengeAsync(CreateGroupChallengeDto dto)
        {
            // Kiểm tra xem nhóm có tồn tại hay không
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == dto.GroupId && g.DeletedAt == null);
            if (!groupExists)
                throw new Exception("Group không tồn tại!");

            var challenge = new GroupChallenge
            {
                GroupId = dto.GroupId,
                Title = dto.Title,
                Description = dto.Description,
                // Đảm bảo đồng bộ chuẩn múi giờ lưu xuống Postgres
                StartsAt = dto.StartsAt.ToUniversalTime(),
                EndsAt = dto.EndsAt.ToUniversalTime(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.GroupChallenges.Add(challenge);
            await _context.SaveChangesAsync();
            return challenge;
        }

        public async Task<IEnumerable<GroupChallenge>> GetChallengesByGroupAsync(Guid groupId)
        {
            return await _context.GroupChallenges
                .Where(c => c.GroupId == groupId && c.DeletedAt == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<GroupChallenge?> GetChallengeByIdAsync(Guid id)
        {
            return await _context.GroupChallenges
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
        }

        public async Task<GroupChallenge> UpdateChallengeAsync(Guid id, UpdateGroupChallengeDto dto)
        {
            var challenge = await _context.GroupChallenges
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (challenge == null)
                throw new Exception("Thử thách không tồn tại hoặc đã bị xóa!");

            challenge.Title = dto.Title;
            challenge.Description = dto.Description;
            challenge.StartsAt = dto.StartsAt.ToUniversalTime();
            challenge.EndsAt = dto.EndsAt.ToUniversalTime();
            challenge.IsActive = dto.IsActive;
            challenge.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return challenge;
        }

        public async Task<bool> DeleteChallengeAsync(Guid id)
        {
            var challenge = await _context.GroupChallenges
                .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

            if (challenge == null)
                return false;

            // Soft delete đồng bộ hệ thống
            challenge.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}