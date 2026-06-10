using HealthPath.API.Extensions;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthPath.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        [Authorize] // BẮT BUỘC có Token JWT mới qua được cửa này
        public async Task<IActionResult> GetMe()
        {
            // 1. Tự động bóc ID của User từ trong Token ra
            var userId = User.GetUserId();

            // 2. Chui vào Database lấy thông tin
            var user = await _userService.GetMeAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            // 3. Trả về cho Mobile
            return Ok(user);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserProfileDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.GetUserId();
            var user = await _userService.UpdateMeAsync(userId, request);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            return Ok(user);
        }
    }
}