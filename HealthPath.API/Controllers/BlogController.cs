using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthPath.API.Common;
using HealthPath.API.Services;

namespace HealthPath.API.Controllers;

[ApiController]
[Route("api")]
[AllowAnonymous]
public class BlogController : ControllerBase
{
    private readonly IBlogService _blogService;

    public BlogController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet("blog-categories")]
    public async Task<IActionResult> GetActiveCategories()
    {
        var response = await _blogService.GetActiveCategoriesAsync();
        return Ok(response);
    }

    [HttpGet("blogs")]
    public async Task<IActionResult> GetBlogs(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var response = await _blogService.GetBlogsAsync(categoryId, search, page, pageSize);
        return Ok(response);
    }

    [HttpGet("blogs/{slug}")]
    public async Task<IActionResult> GetBlogBySlug(string slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return BadRequest(ApiResponse<object>.Fail("Slug bài viết không hợp lệ.", ErrorCode.VALIDATION_ERROR));
        }

        var response = await _blogService.GetBlogBySlugAsync(slug);
        if (!response.Success)
        {
            return NotFound(response);
        }
        return Ok(response);
    }
}
