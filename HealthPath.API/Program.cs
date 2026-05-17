using HealthPath.API.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// khi nào có thằng đòi IUserService, quăng cái MockUserService cho nó
// Sau này có Database, chỉ việc sửa mỗi chữ MockUserService thành SqlUserService ở dòng này là xong hệ thống.
builder.Services.AddScoped<IUserService, MockUserService>();

builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
