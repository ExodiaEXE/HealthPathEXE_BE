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

            // Tự động kiểm tra cột mốc chuỗi ngày để tạo thông báo chúc mừng hệ thống
            if (currentStreak == 3 || currentStreak == 7 || currentStreak == 30)
            {
                try
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Type = "Milestone",
                        Title = "Cột mốc chuỗi ngày mới!",
                        Body = $"Chúc mừng bạn đã đạt chuỗi {currentStreak} ngày ghi nhật ký cảm xúc liên tục! Hãy duy trì phong độ nhé.",
                        Data = "{}",
                        Channel = "InApp",
                        IsRead = false,
                        SentAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // Tránh gây treo luồng chính nếu có lỗi phát sinh cục bộ
                }
            }

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

        public async Task<ApiResponse<MoodCheckinDto>> GetByIdAsync(Guid id, Guid userId)
        {
            var checkin = await _context.MoodCheckins
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.DeletedAt == null);

            if (checkin == null)
            {
                return ApiResponse<MoodCheckinDto>.Fail("Không tìm thấy nhật ký cảm xúc này hoặc bản ghi đã bị xóa.", "MOOD_NOT_FOUND");
            }

            var dto = new MoodCheckinDto
            {
                Id = checkin.Id,
                Mood = checkin.Mood,
                EnergyLevel = checkin.EnergyLevel,
                StreakDay = checkin.StreakDay,
                Note = checkin.Note,
                CheckedAt = checkin.CheckedAt
            };

            return ApiResponse<MoodCheckinDto>.Ok(dto, "Lấy chi tiết nhật ký cảm xúc thành công.");
        }

        public async Task<ApiResponse<MoodCheckinDto>> UpdateCheckinAsync(Guid id, Guid userId, UpdateMoodCheckinDto dto)
        {
            var checkin = await _context.MoodCheckins
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.DeletedAt == null);

            if (checkin == null)
            {
                return ApiResponse<MoodCheckinDto>.Fail("Không tìm thấy dữ liệu nhật ký cần cập nhật.", "MOOD_NOT_FOUND");
            }

            // Quy tắc nghiệp vụ: Chỉ cho phép sửa đổi dữ liệu nếu nhật ký được tạo trong ngày hôm nay
            if (checkin.CheckedAt.Date != DateTime.UtcNow.Date)
            {
                return ApiResponse<MoodCheckinDto>.Fail("Hệ thống bảo mật chặn thao tác: Bạn chỉ được phép sửa nhật ký cảm xúc đã tạo trong ngày hôm nay.", "VALIDATION_ERROR");
            }

            checkin.Mood = dto.Mood;
            checkin.EnergyLevel = dto.EnergyLevel;
            checkin.Note = dto.Note;

            await _context.SaveChangesAsync();

            var resultDto = new MoodCheckinDto
            {
                Id = checkin.Id,
                Mood = checkin.Mood,
                EnergyLevel = checkin.EnergyLevel,
                StreakDay = checkin.StreakDay,
                Note = checkin.Note,
                CheckedAt = checkin.CheckedAt
            };

            return ApiResponse<MoodCheckinDto>.Ok(resultDto, "Cập nhật thông tin nhật ký thành công.");
        }

        public async Task<ApiResponse<object>> DeleteCheckinAsync(Guid id, Guid userId)
        {
            var checkin = await _context.MoodCheckins
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.DeletedAt == null);

            if (checkin == null)
            {
                return ApiResponse<object>.Fail("Bản ghi không tồn tại hoặc đã bị xóa từ trước.", "MOOD_NOT_FOUND");
            }

            // Thực hiện Soft Delete để bảo toàn tính toàn vẹn dữ liệu lịch sử
            checkin.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<object>.Ok(new { }, "Xóa nhật ký cảm xúc thành công.");
        }

        public async Task<ApiResponse<MoodStatsDto>> GetStreakStatsAsync(Guid userId)
        {
            var checkins = await _context.MoodCheckins
                .Where(m => m.UserId == userId && m.DeletedAt == null)
                .OrderBy(m => m.CheckedAt)
                .ToListAsync();

            int totalCheckins = checkins.Count;
            int maxStreak = 0;
            int currentStreak = 0;

            if (totalCheckins > 0)
            {
                var lastCheckin = checkins[totalCheckins - 1];
                var today = DateTime.UtcNow.Date;

                if (lastCheckin.CheckedAt.Date == today || lastCheckin.CheckedAt.Date == today.AddDays(-1))
                {
                    currentStreak = lastCheckin.StreakDay;
                }

                int tempStreak = 0;
                DateTime? prevDate = null;

                foreach (var c in checkins)
                {
                    var currentDate = c.CheckedAt.Date;
                    if (prevDate == null)
                    {
                        tempStreak = 1;
                    }
                    else if (currentDate == prevDate.Value.AddDays(1))
                    {
                        tempStreak++;
                    }
                    else if (currentDate != prevDate.Value)
                    {
                        tempStreak = 1;
                    }

                    if (tempStreak > maxStreak)
                    {
                        maxStreak = tempStreak;
                    }
                    prevDate = currentDate;
                }
            }

            var stats = new MoodStatsDto
            {
                CurrentStreak = currentStreak,
                BestStreak = maxStreak > currentStreak ? maxStreak : currentStreak,
                TotalCheckins = totalCheckins
            };

            return ApiResponse<MoodStatsDto>.Ok(stats, "Lấy số liệu thống kê chuỗi ngày thành công.");
        }
    }
}