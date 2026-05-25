using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Extensions;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthPath.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var response = await _subscriptionService.GetPlansAsync();
        return Ok(response);
    }

    [HttpGet("my-subscription")]
    [Authorize]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = User.GetUserId();

        var response = await _subscriptionService.GetCurrentSubscriptionAsync(userId);
        return Ok(response);
    }

    [HttpGet("my-transactions")]
    [Authorize]
    public async Task<IActionResult> GetMyTransactions()
    {
        var userId = User.GetUserId();

        var response = await _subscriptionService.GetMyTransactionsAsync(userId);
        return Ok(response);
    }

    [HttpPost("verify-receipt")]
    [Authorize]
    public async Task<IActionResult> VerifyReceipt([FromBody] VerifyReceiptRequestDto request)
    {

        var userId = User.GetUserId();

        var response = await _subscriptionService.VerifyAndFulfillPurchaseAsync(userId, request);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
