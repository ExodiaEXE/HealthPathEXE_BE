using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HealthPath.API.Services;

public class AdminAuthService : IAdminAuthService
{
    private readonly HealthpathDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminAuthService(HealthpathDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<ApiResponse<AdminAuthResponseDto>> LoginAsync(AdminLoginDto request)
    {
        // 1. Query the admin
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Username == request.Username && a.IsActive);

        if (admin == null)
        {
            return ApiResponse<AdminAuthResponseDto>.Fail("Tên đăng nhập hoặc mật khẩu không đúng.", ErrorCode.INVALID_CREDENTIALS);
        }

        // 2. Verify BCrypt password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
        {
            return ApiResponse<AdminAuthResponseDto>.Fail("Tên đăng nhập hoặc mật khẩu không đúng.", ErrorCode.INVALID_CREDENTIALS);
        }

        // 3. Update last login
        admin.LastLoginAt = DateTime.UtcNow;
        _context.Admins.Update(admin);
        await _context.SaveChangesAsync();

        // 4. Generate token
        string token = GenerateAdminJwtToken(admin);

        var responseDto = new AdminAuthResponseDto
        {
            Token = token,
            Username = admin.Username,
            FullName = admin.FullName,
            Role = admin.Role
        };

        return ApiResponse<AdminAuthResponseDto>.Ok(responseDto, "Đăng nhập admin thành công!");
    }

    public async Task<ApiResponse<bool>> CreateAdminAsync(CreateAdminDto request)
    {
        // 1. Check duplicate username
        if (await _context.Admins.AnyAsync(a => a.Username == request.Username))
        {
            return ApiResponse<bool>.Fail("Tên đăng nhập đã tồn tại trên hệ thống.", ErrorCode.EMAIL_TAKEN);
        }

        // 2. Hash Password
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 3. Create Admin
        var newAdmin = new Admin
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = passwordHash,
            FullName = request.FullName,
            Email = request.Email,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Admins.Add(newAdmin);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Tạo tài khoản admin mới thành công!");
    }

    private string GenerateAdminJwtToken(Admin admin)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim("FullName", admin.FullName),
            new Claim("Role", admin.Role),
            new Claim("IsAdmin", "true")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7), // Session stays alive for 7 days
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
