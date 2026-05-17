using HealthPath.API.Models;

namespace HealthPath.API.Services
{
    // Bất cứ ai (dữ liệu giả hay dữ liệu SQL) muốn làm Service này đều BẮT BUỘC phải có hàm GetAllUsersForAdmin()
    public interface IUserService
    {
        IEnumerable<UserDto> GetAllUsersForAdmin();
    }
}