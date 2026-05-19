namespace HealthPath.API.Models
{
    public class RegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        // Bất kỳ email nào (kể cả không phải đuôi trường) đều đăng ký được
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty; // Cái này để Mobile cầm đi làm mộc thông hành
    }
}