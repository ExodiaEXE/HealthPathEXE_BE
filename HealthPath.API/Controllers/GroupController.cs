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
    [Authorize] // Bảo mật bằng Token
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly ICompanionService _companionService;

        public GroupController(IGroupService groupService, ICompanionService companionService)
        {
            _groupService = groupService;
            _companionService = companionService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGroupDto dto)
        {
            var userId = User.GetUserId();
            var result = await _groupService.CreateGroupAsync(userId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("my-groups")]
        public async Task<IActionResult> GetMyGroups()
        {
            var userId = User.GetUserId();
            var result = await _groupService.GetMyGroupsAsync(userId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _groupService.GetByIdAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGroupDto dto)
        {
            var userId = User.GetUserId();
            var result = await _groupService.UpdateGroupAsync(id, userId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _groupService.DeleteGroupAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/join")]
        public async Task<IActionResult> Join(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _groupService.JoinGroupAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicGroups([FromQuery] string? search)
        {
            var userId = User.GetUserId();
            var result = await _groupService.GetPublicGroupsAsync(userId, search);
            return Ok(result);
        }

        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetMembers(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _groupService.GetGroupMembersAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("join-by-invite")]
        public async Task<IActionResult> JoinByInviteCode([FromBody] JoinGroupByInviteCodeDto dto)
        {
            var userId = User.GetUserId();
            var result = await _groupService.JoinGroupByInviteCodeAsync(userId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/leave")]
        public async Task<IActionResult> Leave(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _groupService.LeaveGroupAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id}/check-in")]
        public async Task<IActionResult> CheckIn(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _groupService.CheckInGroupAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            try
            {
                await _companionService.ReportEventAsync(userId, "group_checkin");
            }
            catch
            {
                // Điểm danh đã lưu — không fail request vì companion mission.
            }
            return Ok(result);
        }
    }
}