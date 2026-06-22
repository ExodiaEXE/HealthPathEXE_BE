using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public class BlogService : IBlogService
{
    private readonly HealthpathDbContext _context;
    private readonly ILogger<BlogService> _logger;

    public BlogService(HealthpathDbContext context, ILogger<BlogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // --- Helper Methods ---

    private async Task<string> GenerateUniqueCategorySlugAsync(string name, Guid? excludeId = null)
    {
        string baseSlug = SlugHelper.GenerateSlug(name);
        string slug = baseSlug;
        int counter = 1;

        while (await _context.BlogCategories.AnyAsync(c => c.Slug == slug && c.Id != excludeId && c.DeletedAt == null))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private async Task<string> GenerateUniqueBlogSlugAsync(string title, Guid? excludeId = null)
    {
        string baseSlug = SlugHelper.GenerateSlug(title);
        string slug = baseSlug;
        int counter = 1;

        while (await _context.Blogs.AnyAsync(b => b.Slug == slug && b.Id != excludeId && b.DeletedAt == null))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    // --- BlogCategory Admin Methods ---

    public async Task<ApiResponse<List<BlogCategoryDto>>> GetAllCategoriesAdminAsync()
    {
        var categories = await _context.BlogCategories
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.Name)
            .Select(c => new BlogCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<BlogCategoryDto>>.Ok(categories);
    }

    public async Task<ApiResponse<BlogCategoryDto>> GetCategoryByIdAdminAsync(Guid id)
    {
        var category = await _context.BlogCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

        if (category == null)
        {
            return ApiResponse<BlogCategoryDto>.Fail("Không tìm thấy danh mục bài viết.", ErrorCode.BLOG_CATEGORY_NOT_FOUND);
        }

        var dto = new BlogCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };

        return ApiResponse<BlogCategoryDto>.Ok(dto);
    }

    public async Task<ApiResponse<BlogCategoryDto>> CreateCategoryAsync(CreateBlogCategoryDto dto)
    {
        if (await _context.BlogCategories.AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.DeletedAt == null))
        {
            return ApiResponse<BlogCategoryDto>.Fail("Tên danh mục này đã tồn tại.", ErrorCode.BLOG_CATEGORY_NAME_TAKEN);
        }

        string slug = await GenerateUniqueCategorySlugAsync(dto.Name);

        var category = new BlogCategory
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BlogCategories.Add(category);
        await _context.SaveChangesAsync();

        var resultDto = new BlogCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };

        return ApiResponse<BlogCategoryDto>.Ok(resultDto, "Tạo danh mục bài viết thành công.");
    }

    public async Task<ApiResponse<BlogCategoryDto>> UpdateCategoryAsync(Guid id, UpdateBlogCategoryDto dto)
    {
        var category = await _context.BlogCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

        if (category == null)
        {
            return ApiResponse<BlogCategoryDto>.Fail("Không tìm thấy danh mục bài viết.", ErrorCode.BLOG_CATEGORY_NOT_FOUND);
        }

        if (await _context.BlogCategories.AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.Id != id && c.DeletedAt == null))
        {
            return ApiResponse<BlogCategoryDto>.Fail("Tên danh mục này đã tồn tại ở bản ghi khác.", ErrorCode.BLOG_CATEGORY_NAME_TAKEN);
        }

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.IsActive = dto.IsActive;
        category.Slug = await GenerateUniqueCategorySlugAsync(dto.Name, id);
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var resultDto = new BlogCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };

        return ApiResponse<BlogCategoryDto>.Ok(resultDto, "Cập nhật danh mục bài viết thành công.");
    }

    public async Task<ApiResponse<bool>> DeleteCategoryAsync(Guid id)
    {
        var category = await _context.BlogCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

        if (category == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy danh mục bài viết.", ErrorCode.BLOG_CATEGORY_NOT_FOUND);
        }

        // Kiểm tra xem có bài viết nào thuộc danh mục này mà chưa bị xóa không
        bool hasBlogs = await _context.Blogs.AnyAsync(b => b.CategoryId == id && b.DeletedAt == null);
        if (hasBlogs)
        {
            return ApiResponse<bool>.Fail("Không thể xóa danh mục đang có chứa bài viết.", ErrorCode.VALIDATION_ERROR);
        }

        category.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Xóa danh mục bài viết thành công.");
    }

    // --- BlogCategory User Methods ---

    public async Task<ApiResponse<List<BlogCategoryDto>>> GetActiveCategoriesAsync()
    {
        var categories = await _context.BlogCategories
            .Where(c => c.IsActive && c.DeletedAt == null)
            .OrderBy(c => c.Name)
            .Select(c => new BlogCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<BlogCategoryDto>>.Ok(categories);
    }

    // --- Blog Admin Methods ---

    public async Task<ApiResponse<PageResponse<BlogDto>>> GetBlogsAdminAsync(Guid? categoryId, string? search, int page, int pageSize)
    {
        var query = _context.Blogs
            .Include(b => b.Category)
            .Where(b => b.DeletedAt == null);

        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(searchLower) || (b.Summary != null && b.Summary.ToLower().Contains(searchLower)));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BlogDto
            {
                Id = b.Id,
                Title = b.Title,
                Slug = b.Slug,
                Summary = b.Summary,
                ThumbnailUrl = b.ThumbnailUrl,
                CategoryId = b.CategoryId,
                CategoryName = b.Category.Name,
                Views = b.Views,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        var pageResponse = new PageResponse<BlogDto>(items, totalItems, page, pageSize);
        return ApiResponse<PageResponse<BlogDto>>.Ok(pageResponse);
    }

    public async Task<ApiResponse<BlogDetailDto>> GetBlogByIdAdminAsync(Guid id)
    {
        var blog = await _context.Blogs
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

        if (blog == null)
        {
            return ApiResponse<BlogDetailDto>.Fail("Không tìm thấy bài viết.", ErrorCode.BLOG_NOT_FOUND);
        }

        var dto = new BlogDetailDto
        {
            Id = blog.Id,
            Title = blog.Title,
            Slug = blog.Slug,
            Body = blog.Body,
            Summary = blog.Summary,
            ThumbnailUrl = blog.ThumbnailUrl,
            CategoryId = blog.CategoryId,
            CategoryName = blog.Category.Name,
            Views = blog.Views,
            IsActive = blog.IsActive,
            CreatedBy = blog.CreatedBy,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt
        };

        return ApiResponse<BlogDetailDto>.Ok(dto);
    }

    public async Task<ApiResponse<BlogDetailDto>> CreateBlogAsync(CreateBlogDto dto, Guid? adminId)
    {
        var categoryExists = await _context.BlogCategories.AnyAsync(c => c.Id == dto.CategoryId && c.DeletedAt == null);
        if (!categoryExists)
        {
            return ApiResponse<BlogDetailDto>.Fail("Danh mục bài viết không tồn tại.", ErrorCode.BLOG_CATEGORY_NOT_FOUND);
        }

        string slug = await GenerateUniqueBlogSlugAsync(dto.Title);

        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Slug = slug,
            Body = dto.Body,
            Summary = dto.Summary,
            ThumbnailUrl = dto.ThumbnailUrl,
            CategoryId = dto.CategoryId,
            IsActive = dto.IsActive,
            Views = 0,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Blogs.Add(blog);
        await _context.SaveChangesAsync();

        // Nạp thêm Category Name cho DTO kết quả
        var categoryName = await _context.BlogCategories
            .Where(c => c.Id == blog.CategoryId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();

        var resultDto = new BlogDetailDto
        {
            Id = blog.Id,
            Title = blog.Title,
            Slug = blog.Slug,
            Body = blog.Body,
            Summary = blog.Summary,
            ThumbnailUrl = blog.ThumbnailUrl,
            CategoryId = blog.CategoryId,
            CategoryName = categoryName ?? "",
            Views = blog.Views,
            IsActive = blog.IsActive,
            CreatedBy = blog.CreatedBy,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt
        };

        return ApiResponse<BlogDetailDto>.Ok(resultDto, "Tạo bài viết thành công.");
    }

    public async Task<ApiResponse<BlogDetailDto>> UpdateBlogAsync(Guid id, UpdateBlogDto dto)
    {
        var blog = await _context.Blogs
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

        if (blog == null)
        {
            return ApiResponse<BlogDetailDto>.Fail("Không tìm thấy bài viết cần cập nhật.", ErrorCode.BLOG_NOT_FOUND);
        }

        var categoryExists = await _context.BlogCategories.AnyAsync(c => c.Id == dto.CategoryId && c.DeletedAt == null);
        if (!categoryExists)
        {
            return ApiResponse<BlogDetailDto>.Fail("Danh mục bài viết được cập nhật không tồn tại.", ErrorCode.BLOG_CATEGORY_NOT_FOUND);
        }

        blog.Title = dto.Title;
        blog.Body = dto.Body;
        blog.Summary = dto.Summary;
        blog.ThumbnailUrl = dto.ThumbnailUrl;
        blog.CategoryId = dto.CategoryId;
        blog.IsActive = dto.IsActive;
        blog.Slug = await GenerateUniqueBlogSlugAsync(dto.Title, id);
        blog.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var categoryName = await _context.BlogCategories
            .Where(c => c.Id == blog.CategoryId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();

        var resultDto = new BlogDetailDto
        {
            Id = blog.Id,
            Title = blog.Title,
            Slug = blog.Slug,
            Body = blog.Body,
            Summary = blog.Summary,
            ThumbnailUrl = blog.ThumbnailUrl,
            CategoryId = blog.CategoryId,
            CategoryName = categoryName ?? "",
            Views = blog.Views,
            IsActive = blog.IsActive,
            CreatedBy = blog.CreatedBy,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt
        };

        return ApiResponse<BlogDetailDto>.Ok(resultDto, "Cập nhật bài viết thành công.");
    }

    public async Task<ApiResponse<bool>> DeleteBlogAsync(Guid id)
    {
        var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);
        if (blog == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy bài viết.", ErrorCode.BLOG_NOT_FOUND);
        }

        blog.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Xóa bài viết thành công.");
    }

    public async Task<ApiResponse<BlogDetailDto>> ToggleBlogActiveAsync(Guid id)
    {
        var blog = await _context.Blogs
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);

        if (blog == null)
        {
            return ApiResponse<BlogDetailDto>.Fail("Không tìm thấy bài viết.", ErrorCode.BLOG_NOT_FOUND);
        }

        blog.IsActive = !blog.IsActive;
        blog.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var dto = new BlogDetailDto
        {
            Id = blog.Id,
            Title = blog.Title,
            Slug = blog.Slug,
            Body = blog.Body,
            Summary = blog.Summary,
            ThumbnailUrl = blog.ThumbnailUrl,
            CategoryId = blog.CategoryId,
            CategoryName = blog.Category.Name,
            Views = blog.Views,
            IsActive = blog.IsActive,
            CreatedBy = blog.CreatedBy,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt
        };

        return ApiResponse<BlogDetailDto>.Ok(dto, $"Đã {(blog.IsActive ? "kích hoạt" : "vô hiệu hóa")} bài viết thành công.");
    }

    // --- Blog User Methods ---

    public async Task<ApiResponse<PageResponse<BlogDto>>> GetBlogsAsync(Guid? categoryId, string? search, int page, int pageSize)
    {
        var query = _context.Blogs
            .Include(b => b.Category)
            .Where(b => b.IsActive && b.DeletedAt == null && b.Category.IsActive && b.Category.DeletedAt == null);

        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(searchLower) || (b.Summary != null && b.Summary.ToLower().Contains(searchLower)));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BlogDto
            {
                Id = b.Id,
                Title = b.Title,
                Slug = b.Slug,
                Summary = b.Summary,
                ThumbnailUrl = b.ThumbnailUrl,
                CategoryId = b.CategoryId,
                CategoryName = b.Category.Name,
                Views = b.Views,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();

        var pageResponse = new PageResponse<BlogDto>(items, totalItems, page, pageSize);
        return ApiResponse<PageResponse<BlogDto>>.Ok(pageResponse);
    }

    public async Task<ApiResponse<BlogDetailDto>> GetBlogBySlugAsync(string slug)
    {
        var blog = await _context.Blogs
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Slug.ToLower() == slug.ToLower() && b.IsActive && b.DeletedAt == null && b.Category.IsActive && b.Category.DeletedAt == null);

        if (blog == null)
        {
            return ApiResponse<BlogDetailDto>.Fail("Không tìm thấy bài viết hoặc bài viết đã bị ẩn.", ErrorCode.BLOG_NOT_FOUND);
        }

        // Tăng lượt xem lên 1
        blog.Views++;
        await _context.SaveChangesAsync();

        var dto = new BlogDetailDto
        {
            Id = blog.Id,
            Title = blog.Title,
            Slug = blog.Slug,
            Body = blog.Body,
            Summary = blog.Summary,
            ThumbnailUrl = blog.ThumbnailUrl,
            CategoryId = blog.CategoryId,
            CategoryName = blog.Category.Name,
            Views = blog.Views,
            IsActive = blog.IsActive,
            CreatedBy = blog.CreatedBy,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt
        };

        return ApiResponse<BlogDetailDto>.Ok(dto);
    }
}
