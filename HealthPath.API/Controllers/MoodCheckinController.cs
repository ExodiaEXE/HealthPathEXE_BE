using HealthPath.API.Common;
using HealthPath.API.Extensions;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthPath.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc phải gắn JWT Token mới được gọi
    public class MoodCheckinController : ControllerBase
    {
        private readonly IMoodCheckinService _moodCheckinService;

        public MoodCheckinController(IMoodCheckinService moodCheckinService)
        {
            _moodCheckinService = moodCheckinService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMoodCheckinDto dto)
        {
            var userId = User.GetUserId(); // Móc ID từ Token an toàn tuyệt đối
            var result = await _moodCheckinService.CreateCheckinAsync(userId, dto);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = User.GetUserId();
            var result = await _moodCheckinService.GetMyHistoryAsync(userId);
            return Ok(result);
        }
    }
}