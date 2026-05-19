using HealthPath.API.Models;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 1. Cấu hình Database PostgreSQL
builder.Services.AddDbContext<HealthpathDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Đăng ký các Service (Dependency Injection)
builder.Services.AddScoped<IUserService, MockUserService>(); // Giữ nguyên Mock, sau này viết SqlUserService thì đổi chữ Mock thành Sql là xong
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

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "HealthPath API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Description = "Nhập trực tiếp JWT Token của bạn (không cần gõ chữ 'Bearer' ở trước)",
        Name = "Authorization",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = new System.Collections.Generic.List<string>()
    });
});

var app = builder.Build();

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