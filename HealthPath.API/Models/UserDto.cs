namespace HealthPath.API.Models
{
    // vỏ để chuyển dữ liệu xuống cho FE, không dính tới Database thật
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsPremium { get; set; }
        public bool GoogleLinked { get; set; }
        public bool FacebookLinked { get; set; }
    }
}