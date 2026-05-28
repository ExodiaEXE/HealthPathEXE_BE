using HealthPath.API.Common;
using HealthPath.API.Extensions;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _moodCheckinService.GetByIdAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMoodCheckinDto dto)
        {
            var userId = User.GetUserId();
            var result = await _moodCheckinService.UpdateCheckinAsync(id, userId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _moodCheckinService.DeleteCheckinAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStreakStats()
        {
            var userId = User.GetUserId();
            var result = await _moodCheckinService.GetStreakStatsAsync(userId);
            return Ok(result);
        }
    }
}