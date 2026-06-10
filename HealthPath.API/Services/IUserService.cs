using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services
{
    // Bất cứ ai (dữ liệu giả hay dữ liệu SQL) muốn làm Service này đều BẮT BUỘC phải có hàm GetAllUsersForAdmin()
    public interface IUserService
    {
        IEnumerable<UserDto> GetAllUsersForAdmin();

        //Lấy thông tin của 1 user dựa vào ID
        Task<UserDto?> GetMeAsync(Guid userId);

        Task<UserDto?> UpdateMeAsync(Guid userId, UpdateUserProfileDto request);
    }
}