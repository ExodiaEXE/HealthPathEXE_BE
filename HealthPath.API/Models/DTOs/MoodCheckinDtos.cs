namespace HealthPath.API.Models.DTOs
{
    // Hứng data từ FE gửi lên khi tạo mới
    public class CreateMoodCheckinDto
    {
        public string Mood { get; set; } = null!; // VD: "Happy", "Sad", "Neutral"
        public string EnergyLevel { get; set; } = null!; // VD: "High", "Medium", "Low"
        public string? Note { get; set; }
    }

    // Hứng data từ FE gửi lên khi cập nhật thay đổi nhật ký
    public class UpdateMoodCheckinDto
    {
        public string Mood { get; set; } = null!;
        public string EnergyLevel { get; set; } = null!;
        public string? Note { get; set; }
    }

    // Trả data sạch về cho FE hiển thị
    public class MoodCheckinDto
    {
        public Guid Id { get; set; }
        public string Mood { get; set; } = null!;
        public string EnergyLevel { get; set; } = null!;
        public int StreakDay { get; set; }
        public string? Note { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    // Trả dữ liệu thống kê chuỗi ngày và hiệu suất cho màn hình Profile/Analytics
    public class MoodStatsDto
    {
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public int TotalCheckins { get; set; }
    }
}