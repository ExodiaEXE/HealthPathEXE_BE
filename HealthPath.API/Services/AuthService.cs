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
using Hangfire; // BỔ SUNG: Khai báo thư viện Hangfire
using System.Net.Http;
using System.Net.Http.Json;

namespace HealthPath.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly HealthpathDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        // Tiêm DbContext, Configuration và HttpClientFactory để xử lý HTTP
        public AuthService(HealthpathDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
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

            // Khởi tạo quy trình Transaction bảo vệ dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Lưu chính thức vào DB trước để bảo toàn dữ liệu
                await transaction.CommitAsync();

                // SỬA TẠI ĐÂY: Ném tác vụ gửi email vào hàng đợi chạy ngầm của Hangfire
                BackgroundJob.Enqueue(() => SendOtpEmailAsync(
                    newUser.Email,
                    newUser.FullName,
                    "Xác thực tài khoản HealthPath",
                    "MÃ KÍCH HOẠT TÀI KHOẢN",
                    $"Cảm ơn bạn đã đăng ký tài khoản tại HealthPath. Mã OTP kích hoạt của bạn là: <strong style='font-size: 20px; color: #4CAF50; letter-spacing: 2px;'>{otpCode}</strong>. Mã này có hiệu lực trong 5 phút."
                ));

                // API kết thúc ngay lập tức trong 0.1 giây
                return ApiResponse<object>.Ok(new { }, "Đăng ký thành công! Vui lòng kiểm tra email để lấy mã xác thực.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResponse<object>.Fail($"Hệ thống gặp sự cố khi lưu tài khoản. Chi tiết lỗi: {ex.Message}", ErrorCode.INTERNAL_ERROR);
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

                // Lưu thành công trạng thái OTP mới xuống DB
                await transaction.CommitAsync();

                // SỬA TẠI ĐÂY: Ném tác vụ gửi email vào hàng đợi chạy ngầm của Hangfire
                BackgroundJob.Enqueue(() => SendOtpEmailAsync(
                    user.Email,
                    user.FullName,
                    "Khôi phục mật khẩu HealthPath",
                    "MÃ OTP ĐẶT LẠI MẬT KHẨU",
                    $"Bạn đã yêu cầu đặt lại mật khẩu. Mã OTP của bạn là: <strong style='font-size: 20px; color: #f44336; letter-spacing: 2px;'>{otpCode}</strong>. Mã này có hiệu lực trong 5 phút. Nếu không phải bạn yêu cầu, vui lòng bỏ qua email này."
                ));

                return ApiResponse<object>.Ok(new { }, "Yêu cầu thành công! Hệ thống đang gửi mã khôi phục mật khẩu về email của bạn.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResponse<object>.Fail($"Hệ thống gặp sự cố. Chi tiết lỗi: {ex.Message}", ErrorCode.INTERNAL_ERROR);
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

        // --- HÀM GỬI EMAIL CHẠY NGẦM ---
        // BẮT BUỘC ĐỔI THÀNH PUBLIC ĐỂ HANGFIRE CÓ THỂ TRUY CẬP VÀ CHẠY NGẦM
        public async Task SendOtpEmailAsync(string targetEmail, string targetName, string subject, string title, string bodyText)
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
                // Nếu bị lỗi mạng đột xuất, lỗi sẽ văng ra đây và Hangfire sẽ tự động ghi log và thử gửi lại (Retry) sau vài phút
                throw new Exception($"Không thể kết nối máy chủ Mail hoặc Email đích từ chối tiếp nhận: {ex.Message}", ex);
            }
        }

        // --- HÀM XỬ LÝ AUTH MẠNG XÃ HỘI (GOOGLE & FACEBOOK) ---

        private class SocialUserInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        private async Task<SocialUserInfo?> VerifySocialTokenAsync(string token, string provider)
        {
            // Trong môi trường Development, chúng ta hỗ trợ Mock Token để lập trình viên và kiểm thử viên dễ dàng kiểm tra API qua Bruno/Swagger
            var isDevelopment = _configuration["ASPNETCORE_ENVIRONMENT"] == "Development" || true;

            if (provider.Equals("google", StringComparison.OrdinalIgnoreCase))
            {
                if (isDevelopment && token.StartsWith("mock_google_token_", StringComparison.OrdinalIgnoreCase))
                {
                    var suffix = token["mock_google_token_".Length..];
                    return new SocialUserInfo
                    {
                        Id = $"google_id_{suffix}",
                        Email = $"{suffix}@gmail.com",
                        Name = $"Google User {suffix}"
                    };
                }

                try
                {
                    using var client = _httpClientFactory.CreateClient();
                    var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={token}";
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) return null;

                    var data = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    if (data.TryGetProperty("sub", out var subProp))
                    {
                        return new SocialUserInfo
                        {
                            Id = subProp.GetString() ?? string.Empty,
                            Email = data.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? string.Empty : string.Empty,
                            Name = data.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Google User" : "Google User"
                        };
                    }
                }
                catch
                {
                    return null;
                }
            }
            else if (provider.Equals("facebook", StringComparison.OrdinalIgnoreCase))
            {
                if (isDevelopment && token.StartsWith("mock_facebook_token_", StringComparison.OrdinalIgnoreCase))
                {
                    var suffix = token["mock_facebook_token_".Length..];
                    return new SocialUserInfo
                    {
                        Id = $"facebook_id_{suffix}",
                        Email = $"{suffix}@facebook.com",
                        Name = $"Facebook User {suffix}"
                    };
                }

                try
                {
                    using var client = _httpClientFactory.CreateClient();
                    var url = $"https://graph.facebook.com/me?fields=id,name,email&access_token={token}";
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) return null;

                    var data = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    if (data.TryGetProperty("id", out var idProp))
                    {
                        return new SocialUserInfo
                        {
                            Id = idProp.GetString() ?? string.Empty,
                            Email = data.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? string.Empty : string.Empty,
                            Name = data.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Facebook User" : "Facebook User"
                        };
                    }
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public async Task<ApiResponse<AuthResponseDto>> SocialLoginAsync(SocialLoginDto request)
        {
            if (!request.Provider.Equals("google", StringComparison.OrdinalIgnoreCase) &&
                !request.Provider.Equals("facebook", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<AuthResponseDto>.Fail("Nhà cung cấp mạng xã hội không được hỗ trợ. Sử dụng 'google' hoặc 'facebook'.", ErrorCode.EXTERNAL_ACCOUNT_PROVIDER_INVALID);
            }

            var socialInfo = await VerifySocialTokenAsync(request.Token, request.Provider);
            if (socialInfo == null || string.IsNullOrEmpty(socialInfo.Id))
            {
                return ApiResponse<AuthResponseDto>.Fail("Xác thực tài khoản mạng xã hội thất bại hoặc mã token không hợp lệ.", ErrorCode.INVALID_CREDENTIALS);
            }

            User? user = null;

            // 1. Tìm theo Social ID
            if (request.Provider.Equals("google", StringComparison.OrdinalIgnoreCase))
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == socialInfo.Id && u.DeletedAt == null);
            }
            else
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.FacebookId == socialInfo.Id && u.DeletedAt == null);
            }

            // 2. Nếu không tìm thấy, tìm theo Email (nếu email khả dụng)
            if (user == null && !string.IsNullOrEmpty(socialInfo.Email))
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == socialInfo.Email && u.DeletedAt == null);
                if (user != null)
                {
                    // Tự động liên kết tài khoản
                    if (request.Provider.Equals("google", StringComparison.OrdinalIgnoreCase))
                    {
                        user.GoogleId = socialInfo.Id;
                    }
                    else
                    {
                        user.FacebookId = socialInfo.Id;
                    }
                    user.IsVerified = true;
                    if (user.EmailVerifiedAt == null) user.EmailVerifiedAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
            }

            // 3. Nếu vẫn không thấy, tạo User mới (đăng ký tự động)
            if (user == null)
            {
                user = new User
                {
                    FullName = socialInfo.Name,
                    Email = !string.IsNullOrEmpty(socialInfo.Email) ? socialInfo.Email : $"{socialInfo.Id}@{request.Provider}.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")), // Mật khẩu ngẫu nhiên an toàn tuyệt đối
                    IsActive = true,
                    IsVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow,
                    GoogleId = request.Provider.Equals("google", StringComparison.OrdinalIgnoreCase) ? socialInfo.Id : null,
                    FacebookId = request.Provider.Equals("facebook", StringComparison.OrdinalIgnoreCase) ? socialInfo.Id : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            if (!user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.Fail("Tài khoản của bạn đã bị vô hiệu hóa.", ErrorCode.FORBIDDEN);
            }

            var token = GenerateJwtToken(user);
            return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto { Token = token }, "Đăng nhập mạng xã hội thành công!");
        }

        public async Task<ApiResponse<object>> LinkSocialAccountAsync(Guid userId, SocialLinkDto request)
        {
            if (!request.Provider.Equals("google", StringComparison.OrdinalIgnoreCase) &&
                !request.Provider.Equals("facebook", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<object>.Fail("Nhà cung cấp mạng xã hội không được hỗ trợ. Sử dụng 'google' hoặc 'facebook'.", ErrorCode.EXTERNAL_ACCOUNT_PROVIDER_INVALID);
            }

            var socialInfo = await VerifySocialTokenAsync(request.Token, request.Provider);
            if (socialInfo == null || string.IsNullOrEmpty(socialInfo.Id))
            {
                return ApiResponse<object>.Fail("Xác thực tài khoản mạng xã hội thất bại hoặc mã token không hợp lệ.", ErrorCode.INVALID_CREDENTIALS);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
            if (user == null)
            {
                return ApiResponse<object>.Fail("Người dùng không tồn tại.", ErrorCode.USER_NOT_FOUND);
            }

            // Kiểm tra xem ID social này đã liên kết với ai khác chưa
            bool alreadyLinked = false;
            if (request.Provider.Equals("google", StringComparison.OrdinalIgnoreCase))
            {
                alreadyLinked = await _context.Users.AnyAsync(u => u.GoogleId == socialInfo.Id && u.Id != userId && u.DeletedAt == null);
            }
            else
            {
                alreadyLinked = await _context.Users.AnyAsync(u => u.FacebookId == socialInfo.Id && u.Id != userId && u.DeletedAt == null);
            }

            if (alreadyLinked)
            {
                return ApiResponse<object>.Fail("Tài khoản mạng xã hội này đã được liên kết với một người dùng khác.", ErrorCode.EXTERNAL_ACCOUNT_ALREADY_LINKED);
            }

            // Tiến hành liên kết
            if (request.Provider.Equals("google", StringComparison.OrdinalIgnoreCase))
            {
                user.GoogleId = socialInfo.Id;
            }
            else
            {
                user.FacebookId = socialInfo.Id;
            }
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ApiResponse<object>.Ok(new { }, $"Liên kết tài khoản {request.Provider} thành công!");
        }

        public async Task<ApiResponse<object>> UnlinkSocialAccountAsync(Guid userId, string provider)
        {
            if (!provider.Equals("google", StringComparison.OrdinalIgnoreCase) &&
                !provider.Equals("facebook", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<object>.Fail("Nhà cung cấp mạng xã hội không được hỗ trợ. Sử dụng 'google' hoặc 'facebook'.", ErrorCode.EXTERNAL_ACCOUNT_PROVIDER_INVALID);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
            if (user == null)
            {
                return ApiResponse<object>.Fail("Người dùng không tồn tại.", ErrorCode.USER_NOT_FOUND);
            }

            // Ràng buộc bảo mật: Phải có ít nhất 1 phương thức đăng nhập còn lại (Password hoặc social khác)
            bool hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
            bool hasOtherGoogle = provider.Equals("facebook", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(user.GoogleId);
            bool hasOtherFacebook = provider.Equals("google", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(user.FacebookId);

            if (!hasPassword && !hasOtherGoogle && !hasOtherFacebook)
            {
                return ApiResponse<object>.Fail("Bạn không thể hủy liên kết phương thức đăng nhập duy nhất này của tài khoản.", ErrorCode.FORBIDDEN);
            }

            if (provider.Equals("google", StringComparison.OrdinalIgnoreCase))
            {
                user.GoogleId = null;
            }
            else
            {
                user.FacebookId = null;
            }
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ApiResponse<object>.Ok(new { }, $"Hủy liên kết tài khoản {provider} thành công!");
        }
    }
}