using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthPath.API.Services
{
    // Class này cũng tuân thủ IUserService, nhưng lấy data từ DB thật
    public class SqlUserService : IUserService
    {
        private readonly HealthpathDbContext _context;

        // Tiêm (Inject) DB Context do Entity Framework tự tạo
        public SqlUserService(HealthpathDbContext context)
        {
            _context = context;
        }

        public IEnumerable<UserDto> GetAllUsersForAdmin()
        {
            var usersList = _context.Users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                AvatarUrl = u.AvatarUrl,
                IsPremium = false,
                GoogleLinked = u.GoogleId != null,
                FacebookLinked = u.FacebookId != null,
            }).ToList();

            return usersList;
        }

        public async Task<UserDto?> GetMeAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            return await MapUserDtoAsync(user);
        }

        public async Task<UserDto?> UpdateMeAsync(Guid userId, UpdateUserProfileDto request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            user.FullName = request.FullName.Trim();
            user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await MapUserDtoAsync(user);
        }

        private async Task<UserDto> MapUserDtoAsync(User user)
        {
            var isPremium = await HasPremiumAccessAsync(user.Id);

            return new UserDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                IsPremium = isPremium,
                GoogleLinked = !string.IsNullOrEmpty(user.GoogleId),
                FacebookLinked = !string.IsNullOrEmpty(user.FacebookId),
            };
        }

        private async Task<bool> HasPremiumAccessAsync(Guid userId)
        {
            return await _context.UserSubscriptions
                .AnyAsync(s => s.UserId == userId
                            && s.Status.ToLower() == "active"
                            && (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow)
                            && s.DeletedAt == null);
        }
    }
}
