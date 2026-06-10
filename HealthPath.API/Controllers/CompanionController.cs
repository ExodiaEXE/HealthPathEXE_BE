using System.Threading.Tasks;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.API.Extensions;
using HealthPath.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthPath.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CompanionController : ControllerBase
{
    private readonly ICompanionService _companion;

    public CompanionController(ICompanionService companion)
    {
        _companion = companion;
    }

    [HttpGet("assets")]
    public IActionResult GetAssets()
    {
        var assets = _companion.GetAssets();
        return Ok(ApiResponse<CompanionAssetsDto>.Ok(assets));
    }

    [HttpGet("state")]
    public async Task<IActionResult> GetState()
    {
        var userId = User.GetUserId();
        var res = await _companion.GetStateAsync(userId);
        return res.Success ? Ok(res) : BadRequest(res);
    }

    [HttpPost("feed")]
    public async Task<IActionResult> Feed()
    {
        var userId = User.GetUserId();
        var res = await _companion.FeedAsync(userId);
        return res.Success ? Ok(res) : BadRequest(res);
    }

    [HttpPost("pet")]
    public async Task<IActionResult> Pet()
    {
        var userId = User.GetUserId();
        var res = await _companion.PetAsync(userId);
        return res.Success ? Ok(res) : BadRequest(res);
    }

    [HttpGet("missions")]
    public async Task<IActionResult> GetMissions([FromQuery] string category = "daily")
    {
        var userId = User.GetUserId();
        var res = await _companion.GetMissionsAsync(userId, category);
        return Ok(res);
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog([FromQuery] string? category)
    {
        var userId = User.GetUserId();
        var res = await _companion.GetCatalogAsync(userId, category);
        return Ok(res);
    }

    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] PurchaseCompanionItemDto dto)
    {
        var userId = User.GetUserId();
        var res = await _companion.PurchaseAsync(userId, dto);
        return res.Success ? Ok(res) : BadRequest(res);
    }

    [HttpPost("equip")]
    public async Task<IActionResult> Equip([FromBody] EquipCompanionItemDto dto)
    {
        var userId = User.GetUserId();
        var res = await _companion.EquipAsync(userId, dto);
        return res.Success ? Ok(res) : BadRequest(res);
    }

    [HttpPut("room-theme")]
    public async Task<IActionResult> SetRoomTheme([FromBody] SetRoomThemeDto dto)
    {
        var userId = User.GetUserId();
        var res = await _companion.SetRoomThemeAsync(userId, dto);
        return res.Success ? Ok(res) : BadRequest(res);
    }
}
