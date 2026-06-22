using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface IBlogService
{
    // --- BlogCategory Admin Methods ---
    Task<ApiResponse<List<BlogCategoryDto>>> GetAllCategoriesAdminAsync();
    Task<ApiResponse<BlogCategoryDto>> GetCategoryByIdAdminAsync(Guid id);
    Task<ApiResponse<BlogCategoryDto>> CreateCategoryAsync(CreateBlogCategoryDto dto);
    Task<ApiResponse<BlogCategoryDto>> UpdateCategoryAsync(Guid id, UpdateBlogCategoryDto dto);
    Task<ApiResponse<bool>> DeleteCategoryAsync(Guid id);

    // --- BlogCategory User Methods ---
    Task<ApiResponse<List<BlogCategoryDto>>> GetActiveCategoriesAsync();

    // --- Blog Admin Methods ---
    Task<ApiResponse<PageResponse<BlogDto>>> GetBlogsAdminAsync(Guid? categoryId, string? search, int page, int pageSize);
    Task<ApiResponse<BlogDetailDto>> GetBlogByIdAdminAsync(Guid id);
    Task<ApiResponse<BlogDetailDto>> CreateBlogAsync(CreateBlogDto dto, Guid? adminId);
    Task<ApiResponse<BlogDetailDto>> UpdateBlogAsync(Guid id, UpdateBlogDto dto);
    Task<ApiResponse<bool>> DeleteBlogAsync(Guid id);
    Task<ApiResponse<BlogDetailDto>> ToggleBlogActiveAsync(Guid id);

    // --- Blog User Methods ---
    Task<ApiResponse<PageResponse<BlogDto>>> GetBlogsAsync(Guid? categoryId, string? search, int page, int pageSize);
    Task<ApiResponse<BlogDetailDto>> GetBlogBySlugAsync(string slug);
}
