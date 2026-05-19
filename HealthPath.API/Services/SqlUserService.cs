using HealthPath.API.Models;

namespace HealthPath.API.Services
{
    // Class này cũng tuân thủ IUserService, nhưng lấy data từ DB thật
    public class SqlUserService : IUserService
    {
        private readonly HealthpathDbContext _context;

        // Tiêm (Inject) DB Context do Entity Framework tự tạo
        public SqlUserService(HealthpathDbContext context)
        {
            _context = context;
        }

        public IEnumerable<UserDto> GetAllUsersForAdmin()
        {
            // Dùng LINQ chọc vào bảng Users trong Postgres
            // Đóng gói lại thành UserDto để giấu tuyệt đối PasswordHash và các thông tin nhạy cảm
            var usersList = _context.Users.Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.FullName,
                Email = u.Email,
                // Tạm thời set false, sau này ông ráp logic bảng Subscriptions vào đây sau
                IsPremium = false
            }).ToList();

            return usersList;
        }

        //Tìm user trong DB theo ID
        public async Task<UserDto?> GetMeAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email,
                IsPremium = false
            };
        }
    }
}