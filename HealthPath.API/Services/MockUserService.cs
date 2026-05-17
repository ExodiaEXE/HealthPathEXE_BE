using HealthPath.API.Models;

namespace HealthPath.API.Services
{
    // Class này đồng với IUserService, chuyên trả về dữ liệu giả để test
    public class MockUserService : IUserService
    {
        public IEnumerable<UserDto> GetAllUsersForAdmin()
        {
            return new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), Name = "Nguyen Van A", Email = "a@gmail.com", IsPremium = true },
                new UserDto { Id = Guid.NewGuid(), Name = "Tran Thi B", Email = "b@gmail.com", IsPremium = false },
                new UserDto { Id = Guid.NewGuid(), Name = "Le Van C", Email = "c@gmail.com", IsPremium = false }
            };
        }
    }
}