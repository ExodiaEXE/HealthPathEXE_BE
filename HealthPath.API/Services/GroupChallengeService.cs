using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

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
            // Kiểm tra Group có tồn tại không
            var groupExists = await _context.Groups.AnyAsync(g => g.Id == dto.GroupId && g.DeletedAt == null);
            if (!groupExists)
                throw new Exception("Group không tồn tại!");

            var challenge = new GroupChallenge
            {
                GroupId = dto.GroupId,
                Title = dto.Title,
                Description = dto.Description,
                // Postgres bắt buộc dùng chuẩn giờ UTC
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
    }
}