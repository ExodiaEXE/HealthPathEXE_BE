using HealthPath.API.Middlewares;
using HealthPath.API.Models;
using HealthPath.API.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using HealthPath.API.Extensions;
using dotenv.net;

// Load secrets from .env in project root (env vars override appsettings)
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
{
    DotEnv.Fluent()
        .WithEnvFiles(envFile)
        .WithoutProbeForEnv()
        .Load();
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(x => x.ErrorMessage))
                .ToList();

            var response = HealthPath.API.Common.ApiResponse<object>.Fail(
                "Dữ liệu đầu vào không hợp lệ.",
                HealthPath.API.Common.ErrorCode.VALIDATION_ERROR,
                errors
            );

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
        };
    });

// Register HttpClient for external API calls (e.g., Google/Facebook token verification)
builder.Services.AddHttpClient();

// 1. Cấu hình Database PostgreSQL (giới hạn pool — Supabase session mode ~15 conn)
var postgresConnection = DatabaseConnectionExtensions.NormalizePostgresConnection(
    builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddDbContext<HealthpathDbContext>(options =>
    options.UseNpgsql(postgresConnection, npgsql =>
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(4),
            errorCodesToAdd:
            [
                "57P01", // admin_shutdown
                "53300", // too_many_connections
                "08006", // connection_failure
                "08001", // sqlclient_unable_to_establish_sqlconnection
                "08003", // connection_does_not_exist
                "57P03", // cannot_connect_now
            ])));

// 2. Đăng ký các Service (Dependency Injection)
builder.Services.AddScoped<IUserService, SqlUserService>(); // Giữ nguyên Mock, sau này viết SqlUserService thì đổi chữ Mock thành Sql là xong
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<AuthService>());
builder.Services.AddScoped<IRoutineService, RoutineService>();
builder.Services.AddScoped<IUserRoutineService, UserRoutineService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<HealthPath.API.BackgroundJobs.IRecurringRoutineJob, HealthPath.API.BackgroundJobs.RecurringRoutineJob>();
builder.Services.AddScoped<HealthPath.API.BackgroundJobs.IMissDetectionJob, HealthPath.API.BackgroundJobs.MissDetectionJob>();
builder.Services.AddScoped<HealthPath.API.BackgroundJobs.IDailyCheckinReminderJob, HealthPath.API.BackgroundJobs.DailyCheckinReminderJob>();
builder.Services.AddScoped<HealthPath.API.BackgroundJobs.IRoutineReminderJob, HealthPath.API.BackgroundJobs.RoutineReminderJob>();
builder.Services.AddScoped<ICompanionService, CompanionService>();
builder.Services.Configure<HealthPath.API.Options.CompanionAssetsOptions>(
    builder.Configuration.GetSection(HealthPath.API.Options.CompanionAssetsOptions.SectionName));
builder.Services.Configure<HealthPath.API.Options.SocialAuthOptions>(
    builder.Configuration.GetSection(HealthPath.API.Options.SocialAuthOptions.SectionName));
builder.Services.AddScoped<IMoodCheckinService, MoodCheckinService>();
builder.Services.AddScoped<HealthPath.API.BackgroundJobs.ICompanionDecayJob, HealthPath.API.BackgroundJobs.CompanionDecayJob>();
builder.Services.AddScoped<IGroupChallengeService, GroupChallengeService>();
builder.Services.AddScoped<IGroupService, GroupService>();

// 5. Đăng ký các dịch vụ bổ sung qua Extension Methods (Notification, File Storage, Hangfire)
builder.Services.AddNotificationServices(builder.Configuration);
builder.Services.AddFileStorageServices(builder.Configuration);
var hangfireConnection =
    DatabaseConnectionExtensions.ResolveHangfireConnection(builder.Configuration);
builder.Services.AddHangfireServices(hangfireConnection);
builder.Services.AddSingleton<IBackgroundJobDispatcher, HangfireBackgroundJobDispatcher>();
builder.Services.AddAudioServices();
builder.Services.AddSubscriptionServices();
builder.Services.AddAdminServices();

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

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // SignalR: token qua query ?access_token=...
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
                return Task.CompletedTask;
            }

            // Swagger hay dán thiếu chữ "Bearer " -> tự lấy token cho dễ test
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                context.Token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authHeader["Bearer ".Length..].Trim()
                    : authHeader.Trim();
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("IsAdmin", "true");
    });
});

// <-- CODE BẠN THÊM: Cấu hình Swagger có ổ khóa JWT (Mở rộng từ AddSwaggerGen cũ của ông)
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "1) Gọi POST /api/Auth/login để lấy token. 2) Bấm Authorize, chỉ dán phần token (không cần gõ chữ Bearer).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Cách cấu hình ổ khóa mới tinh dành riêng cho .NET 10
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});


var app = builder.Build();

// Reverse proxy (nginx trên VPS) — cần cho HTTPS termination, không đổi API contract mobile
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Đăng ký Middleware xử lý lỗi tập trung đầu tiên trong pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Áp dụng EF migrations (baseline DB scaffold cũ nếu cần), rồi seed admin
await app.ApplyMigrationsAsync();
await app.SeedDefaultAdminAsync();
await app.SeedDefaultRoutinesAsync();
await app.SeedDefaultSubscriptionPlansAsync();

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
app.MapHub<HealthPath.API.Services.Hubs.NotificationHub>("/hubs/notification");

app.UseHangfireJobs();

app.Run();