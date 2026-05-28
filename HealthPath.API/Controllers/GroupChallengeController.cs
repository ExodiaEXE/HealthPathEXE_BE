using HealthPath.API.Common;
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
    [Authorize] // Bắt buộc User phải có Token JWT
    public class GroupChallengeController : ControllerBase
    {
        private readonly IGroupChallengeService _challengeService;

        public GroupChallengeController(IGroupChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateChallenge([FromBody] CreateGroupChallengeDto dto)
        {
            try
            {
                var result = await _challengeService.CreateChallengeAsync(dto);
                return Ok(ApiResponse<object>.Ok(result, "Tạo thử thách nhóm thành công!"));
            }
            catch (Exception ex)
            {
                // Sử dụng chuỗi string trực tiếp giống hệt luồng MoodCheckin để tránh lỗi CS0117
                return BadRequest(ApiResponse<object>.Fail(ex.Message, "VALIDATION_ERROR"));
            }
        }

        [HttpGet("group/{groupId}/challenges")]
        public async Task<IActionResult> GetChallengesByGroup(Guid groupId)
        {
            var result = await _challengeService.GetChallengesByGroupAsync(groupId);
            return Ok(ApiResponse<object>.Ok(result, "Lấy danh sách thử thách nhóm thành công."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChallengeById(Guid id)
        {
            var result = await _challengeService.GetChallengeByIdAsync(id);
            if (result == null)
                return NotFound(ApiResponse<object>.Fail("Thử thách không tồn tại hoặc đã bị xóa!", "NOT_FOUND"));

            return Ok(ApiResponse<object>.Ok(result, "Lấy chi tiết thử thách thành công."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChallenge(Guid id, [FromBody] UpdateGroupChallengeDto dto)
        {
            try
            {
                var result = await _challengeService.UpdateChallengeAsync(id, dto);
                return Ok(ApiResponse<object>.Ok(result, "Cập nhật thử thách nhóm thành công."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, "VALIDATION_ERROR"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChallenge(Guid id)
        {
            var success = await _challengeService.DeleteChallengeAsync(id);
            if (!success)
                return NotFound(ApiResponse<object>.Fail("Thử thách không tồn tại hoặc đã bị xóa!", "NOT_FOUND"));

            return Ok(ApiResponse<object>.Ok(new { }, "Xóa thử thách nhóm thành công."));
        }
    }
}