using HealthPath.API.Models;
using HealthPath.API.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            // Kiểm tra định dạng Email hợp lệ trước khi thực hiện các logic nghiệp vụ
            if (string.IsNullOrWhiteSpace(request.Email) ||
                !Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
            {
                return ApiResponse<object>.Fail("Định dạng email không hợp lệ!", ErrorCode.VALIDATION_ERROR);
            }

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

            // Sinh mã OTP và gán bổ sung vào user mới trước khi lưu xuống Database
            string otpCode = new Random().Next(100000, 999999).ToString();
            newUser.OtpCode = otpCode;
            newUser.OtpExpiryTime = DateTime.UtcNow.AddMinutes(5);

            // Khởi tạo quy trình Transaction bảo vệ dữ liệu, hủy lưu nếu không thể gửi OTP tới Gmail
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Thực hiện gửi email kích hoạt chứa mã số OTP 6 chữ số
                await SendOtpEmailAsync(
                    newUser.Email,
                    newUser.FullName,
                    "Xác thực tài khoản HealthPath",
                    "MÃ KÍCH HOẠT TÀI KHOẢN",
                    $"Cảm ơn bạn đã đăng ký tài khoản tại HealthPath. Mã OTP kích hoạt của bạn là: <strong style='font-size: 20px; color: #4CAF50; letter-spacing: 2px;'>{otpCode}</strong>. Mã này có hiệu lực trong 5 phút."
                );

                // Nếu luồng gửi thư chạy thành công hoàn toàn, chính thức lưu dữ liệu vào cơ sở dữ liệu
                await transaction.CommitAsync();
                return ApiResponse<object>.Ok(new { }, "Đăng ký thành công!");
            }
            catch (Exception ex)
            {
                // Thực hiện khôi phục lại trạng thái cũ của Database nếu có bất cứ lỗi liên lạc hòm thư nào xảy ra
                await transaction.RollbackAsync();
                return ApiResponse<object>.Fail($"Hệ thống không thể gửi mã xác thực tới Gmail này. Vui lòng kiểm tra lại sự tồn tại của tài khoản Email! Chi tiết lỗi: {ex.Message}", ErrorCode.INTERNAL_ERROR);
            }
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

            // Thêm kiểm tra trạng thái xác thực: Chặn không cho đăng nhập nếu IsVerified = false
            if (!user.IsVerified)
            {
                return ApiResponse<AuthResponseDto>.Fail("Tài khoản chưa được xác thực email. Vui lòng xác thực trước khi đăng nhập!", ErrorCode.EMAIL_NOT_VERIFIED);
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

        // --- CÁC PHƯƠNG THỨC NÂNG CẤP BỔ SUNG ĐỂ SỬ DỤNG OTP VÀ GỬI EMAIL ---

        private async Task SendOtpEmailAsync(string targetEmail, string targetName, string subject, string title, string bodyText)
        {
            var host = _configuration["Smtp:Host"];
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var portStr = _configuration["Smtp:Port"];
            int port = int.TryParse(portStr, out int p) ? p : 587;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new Exception("Cấu hình hệ thống SMTP chưa hoàn thiện.");
            }

            try
            {
                var emailMessage = new MimeMessage();
                var fromName = _configuration["Smtp:FromName"] ?? "HealthPath";
                var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@healthpath.vn";

                emailMessage.From.Add(new MailboxAddress(fromName, fromEmail));
                emailMessage.To.Add(new MailboxAddress(targetName, targetEmail));
                emailMessage.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; color: #333; max-width: 500px; margin: auto; border: 1px solid #eee; border-radius: 5px;'>
                            <h2 style='color: #4CAF50; text-align: center;'>{title}</h2>
                            <p>Xin chào <strong>{targetName}</strong>,</p>
                            <p>{bodyText}</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                            <small style='color: #888; display: block; text-align: center;'>HealthPath — Ứng dụng Quản lý Lối sống Lành mạnh</small>
                        </div>"
                };

                emailMessage.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Ném ngoại lệ ra ngoài để kích hoạt cơ chế Rollback dữ liệu tại các hàm gọi nghiệp vụ
                throw new Exception($"Không thể kết nối máy chủ Mail hoặc Email đích từ chối tiếp nhận: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<object>> VerifyRegisterOtpAsync(VerifyOtpDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.DeletedAt == null);
            if (user == null)
            {
                return ApiResponse<object>.Fail("Người dùng không tồn tại!", ErrorCode.USER_NOT_FOUND);
            }

            if (user.IsVerified)
            {
                return ApiResponse<object>.Fail("Tài khoản này đã được xác thực từ trước.", ErrorCode.VALIDATION_ERROR);
            }

            if (user.OtpCode != request.OtpCode)
            {
                return ApiResponse<object>.Fail("Mã OTP không chính xác!", ErrorCode.INVALID_OTP);
            }

            if (user.OtpExpiryTime < DateTime.UtcNow)
            {
                return ApiResponse<object>.Fail("Mã OTP đã hết hạn!", ErrorCode.OTP_EXPIRED);
            }

            user.IsVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
            user.OtpCode = null;
            user.OtpExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ApiResponse<object>.Ok(new { }, "Xác thực và kích hoạt tài khoản thành công!");
        }

        public async Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.DeletedAt == null);
            if (user == null)
            {
                return ApiResponse<object>.Fail("Email không tồn tại trong hệ thống!", ErrorCode.USER_NOT_FOUND);
            }

            string otpCode = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otpCode;
            user.OtpExpiryTime = DateTime.UtcNow.AddMinutes(5);
            user.UpdatedAt = DateTime.UtcNow;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.SaveChangesAsync();

                await SendOtpEmailAsync(
                    user.Email,
                    user.FullName,
                    "Khôi phục mật khẩu HealthPath",
                    "MÃ OTP ĐẶT LẠI MẬT KHẨU",
                    $"Bạn đã yêu cầu đặt lại mật khẩu. Mã OTP của bạn là: <strong style='font-size: 20px; color: #f44336; letter-spacing: 2px;'>{otpCode}</strong>. Mã này có hiệu lực trong 5 phút. Nếu không phải bạn yêu cầu, vui lòng bỏ qua email này."
                );

                await transaction.CommitAsync();
                return ApiResponse<object>.Ok(new { }, "Mã khôi phục mật khẩu đã được gửi về email của bạn.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResponse<object>.Fail($"Hệ thống không thể gửi mã OTP khôi phục mật khẩu. Chi tiết lỗi: {ex.Message}", ErrorCode.INTERNAL_ERROR);
            }
        }

        public async Task<ApiResponse<object>> ResetPasswordWithOtpAsync(ResetPasswordWithOtpDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.DeletedAt == null);
            if (user == null)
            {
                return ApiResponse<object>.Fail("Người dùng không tồn tại!", ErrorCode.USER_NOT_FOUND);
            }

            if (user.OtpCode != request.OtpCode)
            {
                return ApiResponse<object>.Fail("Mã OTP không chính xác!", ErrorCode.INVALID_OTP);
            }

            if (user.OtpExpiryTime < DateTime.UtcNow)
            {
                return ApiResponse<object>.Fail("Mã OTP đã hết hạn!", ErrorCode.OTP_EXPIRED);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.OtpCode = null;
            user.OtpExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ApiResponse<object>.Ok(new { }, "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.");
        }
    }
}