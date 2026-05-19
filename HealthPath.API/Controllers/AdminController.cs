using HealthPath.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HealthPath.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;

        // Tiêm Interface vào đây. Thằng Controller KHÔNG HỀ BIẾT mình đang dùng dữ liệu thật hay giả.
        // Nó chỉ biết gọi qua Interface. Sự lỏng lẻo này giúp code ông không bao giờ bị cứng ngắc!
        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _userService.GetAllUsersForAdmin();
            return Ok(users);
        }
    }
}