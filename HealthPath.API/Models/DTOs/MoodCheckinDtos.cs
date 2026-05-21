namespace HealthPath.API.Models.DTOs
{
    // Hứng data từ FE gửi lên
    public class CreateMoodCheckinDto
    {
        public string Mood { get; set; } = null!; // VD: "Happy", "Sad", "Neutral"
        public string EnergyLevel { get; set; } = null!; // VD: "High", "Medium", "Low"
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
}