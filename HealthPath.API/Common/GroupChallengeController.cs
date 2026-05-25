using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetChallengesByGroup(Guid groupId)
        {
            var result = await _challengeService.GetChallengesByGroupAsync(groupId);
            return Ok(result);
        }
    }
}