using HealthPath.API.Models;
using HealthPath.API.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HealthPath.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly HealthpathDbContext _context;
        private readonly IConfiguration _configuration;

        // Tiêm DbContext vào để lấy data, tiêm Configuration để lấy Secret Key tạo Token
        public AuthService(HealthpathDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<ApiResponse<object>> RegisterAsync(RegisterDto request)
        {
            // 1. Kiểm tra email trùng
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return ApiResponse<object>.Fail("Email này đã được sử dụng!", ErrorCode.EMAIL_TAKEN);
            }

            // 2. Băm (Hash) mật khẩu - TUYỆT ĐỐI KHÔNG lưu pass trần
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Tạo User mới (Map các thuộc tính theo Class mà lệnh Scaffold đã tự tạo cho ông)
            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = passwordHash,
                IsActive = true, // Mặc định cho hoạt động luôn
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return ApiResponse<object>.Ok(new { }, "Đăng ký thành công!");
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto request)
        {
            // 1. Tìm User
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.Fail("Sai email hoặc mật khẩu!", ErrorCode.INVALID_CREDENTIALS);
            }

            // 2. Kiểm tra mật khẩu có khớp với cục Hash trong DB không
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return ApiResponse<AuthResponseDto>.Fail("Sai email hoặc mật khẩu!", ErrorCode.INVALID_CREDENTIALS);
            }

            // 3. Tạo Token (Thẻ thông hành)
            var token = GenerateJwtToken(user);

            return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto { Token = token }, "Đăng nhập thành công!");
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("FullName", user.FullName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), // Token sống 7 ngày
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}