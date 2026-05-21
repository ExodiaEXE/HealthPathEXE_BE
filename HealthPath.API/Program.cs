using HealthPath.API.Middlewares;
using HealthPath.API.Models;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 1. Cấu hình Database PostgreSQL
builder.Services.AddDbContext<HealthpathDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Đăng ký các Service (Dependency Injection)
builder.Services.AddScoped<IUserService, SqlUserService>(); // Giữ nguyên Mock, sau này viết SqlUserService thì đổi chữ Mock thành Sql là xong
builder.Services.AddScoped<IAuthService, AuthService>();     // <-- MỚI THÊM: Đăng ký Service IAM
builder.Services.AddScoped<IRoutineService, RoutineService>();
builder.Services.AddScoped<IUserRoutineService, UserRoutineService>();

// 3. Mở CORS cho Front-end (Web/Mobile) gọi API không bị chặn
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// 4. <-- MỚI THÊM: Cấu hình giải mã JWT Token (Bảo vệ API)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ValidateIssuer = false, // Đồ án sinh viên để false cho dễ thở, lên thực tế cấu hình sau
        ValidateAudience = false
    };
});

// <-- CODE BẠN THÊM: Cấu hình Swagger có ổ khóa JWT (Mở rộng từ AddSwaggerGen cũ của ông)
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token vào ô bên dưới. Nhớ có chữ 'Bearer ' ở đằng trước nhé. Ví dụ: Bearer eyJhbGci...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Cách cấu hình ổ khóa mới tinh dành riêng cho .NET 10
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});


var app = builder.Build();

// Đăng ký Middleware xử lý lỗi tập trung đầu tiên trong pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll"); // Mở cửa CORS

app.UseHttpsRedirection();

// <-- MỚI THÊM: Bắt buộc UseAuthentication (Xác thực) phải nằm TRƯỚC UseAuthorization (Phân quyền)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();