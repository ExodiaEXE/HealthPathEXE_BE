using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthPath.API.Common;
using HealthPath.API.Extensions;
using HealthPath.API.Filters;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;

namespace HealthPath.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminBlogController : ControllerBase
{
    private readonly IBlogService _blogService;

    public AdminBlogController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    // --- BlogCategory Endpoints ---

    [HttpGet("blog-categories")]
    [RequirePermission("view_blogs")]
    public async Task<IActionResult> GetAllCategories()
    {
        var response = await _blogService.GetAllCategoriesAdminAsync();
        return Ok(response);
    }

    [HttpGet("blog-categories/{id}")]
    [RequirePermission("view_blogs")]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var response = await _blogService.GetCategoryByIdAdminAsync(id);
        if (!response.Success)
        {
            return NotFound(response);
        }
        return Ok(response);
    }

    [HttpPost("blog-categories")]
    [RequirePermission("manage_blogs")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateBlogCategoryDto request)
    {
        var response = await _blogService.CreateCategoryAsync(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPut("blog-categories/{id}")]
    [RequirePermission("manage_blogs")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateBlogCategoryDto request)
    {
        var response = await _blogService.UpdateCategoryAsync(id, request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpDelete("blog-categories/{id}")]
    [RequirePermission("manage_blogs")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var response = await _blogService.DeleteCategoryAsync(id);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    // --- Blog Endpoints ---

    [HttpGet("blogs")]
    [RequirePermission("view_blogs")]
    public async Task<IActionResult> GetBlogs(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var response = await _blogService.GetBlogsAdminAsync(categoryId, search, page, pageSize);
        return Ok(response);
    }

    [HttpGet("blogs/{id}")]
    [RequirePermission("view_blogs")]
    public async Task<IActionResult> GetBlogById(Guid id)
    {
        var response = await _blogService.GetBlogByIdAdminAsync(id);
        if (!response.Success)
        {
            return NotFound(response);
        }
        return Ok(response);
    }

    [HttpPost("blogs")]
    [RequirePermission("manage_blogs")]
    public async Task<IActionResult> CreateBlog([FromBody] CreateBlogDto request)
    {
        Guid adminId = User.GetUserId();
        var response = await _blogService.CreateBlogAsync(request, adminId);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPut("blogs/{id}")]
    [RequirePermission("manage_blogs")]
    public async Task<IActionResult> UpdateBlog(Guid id, [FromBody] UpdateBlogDto request)
    {
        var response = await _blogService.UpdateBlogAsync(id, request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpDelete("blogs/{id}")]
    [RequirePermission("manage_blogs")]
    public async Task<IActionResult> DeleteBlog(Guid id)
    {
        var response = await _blogService.DeleteBlogAsync(id);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPut("blogs/{id}/toggle-active")]
    [RequirePermission("manage_blogs")]
    public async Task<IActionResult> ToggleBlogActive(Guid id)
    {
        var response = await _blogService.ToggleBlogActiveAsync(id);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }
}
