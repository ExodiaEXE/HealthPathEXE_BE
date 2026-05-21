using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HealthPath.API.Services
{
    public class MoodCheckinService : IMoodCheckinService
    {
        private readonly HealthpathDbContext _context;

        public MoodCheckinService(HealthpathDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<MoodCheckinDto>> CreateCheckinAsync(Guid userId, CreateMoodCheckinDto dto)
        {
            // Logic tính Chuỗi ngày (StreakDay)
            // Tìm lần checkin gần nhất của ông này
            var lastCheckin = await _context.MoodCheckins
                .Where(m => m.UserId == userId && m.DeletedAt == null)
                .OrderByDescending(m => m.CheckedAt)
                .FirstOrDefaultAsync();

            int currentStreak = 1; // Mặc định là ngày 1
            var today = DateTime.UtcNow.Date;

            if (lastCheckin != null)
            {
                var lastCheckinDate = lastCheckin.CheckedAt.Date;

                if (lastCheckinDate == today)
                {
                    return ApiResponse<MoodCheckinDto>.Fail("Hôm nay bạn đã ghi nhật ký cảm xúc rồi!", "ALREADY_CHECKED_IN");
                }
                else if (lastCheckinDate == today.AddDays(-1))
                {
                    // Hôm qua có checkin -> Tăng chuỗi lên 1
                    currentStreak = lastCheckin.StreakDay + 1;
                }
                // Nếu bỏ lỡ hôm qua, chuỗi tự reset về 1
            }

            // Tạo bản ghi mới chuẩn theo Model của anh Hùng
            var newCheckin = new MoodCheckin
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Mood = dto.Mood,
                EnergyLevel = dto.EnergyLevel,
                Note = dto.Note,
                StreakDay = currentStreak,
                CheckedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
                // DeletedAt để trống vì chưa bị xóa
            };

            _context.MoodCheckins.Add(newCheckin);
            await _context.SaveChangesAsync();

            // Map ra DTO để trả về
            var resultDto = new MoodCheckinDto
            {
                Id = newCheckin.Id,
                Mood = newCheckin.Mood,
                EnergyLevel = newCheckin.EnergyLevel,
                StreakDay = newCheckin.StreakDay,
                Note = newCheckin.Note,
                CheckedAt = newCheckin.CheckedAt
            };

            return ApiResponse<MoodCheckinDto>.Ok(resultDto, $"Đã lưu tâm trạng! Chuỗi liên tục: {currentStreak} ngày 🔥");
        }

        public async Task<ApiResponse<List<MoodCheckinDto>>> GetMyHistoryAsync(Guid userId)
        {
            // Lấy danh sách, loại bỏ những cái đã bị xóa (DeletedAt != null)
            var history = await _context.MoodCheckins
                .Where(m => m.UserId == userId && m.DeletedAt == null)
                .OrderByDescending(m => m.CheckedAt)
                .Select(m => new MoodCheckinDto
                {
                    Id = m.Id,
                    Mood = m.Mood,
                    EnergyLevel = m.EnergyLevel,
                    StreakDay = m.StreakDay,
                    Note = m.Note,
                    CheckedAt = m.CheckedAt
                })
                .ToListAsync();

            return ApiResponse<List<MoodCheckinDto>>.Ok(history, "Lấy lịch sử thành công");
        }
    }
}