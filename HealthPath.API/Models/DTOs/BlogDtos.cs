using System;
using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models.DTOs;

// --- BlogCategory DTOs ---

public class BlogCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBlogCategoryDto
{
    [Required(ErrorMessage = "Tên danh mục không được để trống.")]
    [MaxLength(150, ErrorMessage = "Tên danh mục không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateBlogCategoryDto
{
    [Required(ErrorMessage = "Tên danh mục không được để trống.")]
    [MaxLength(150, ErrorMessage = "Tên danh mục không được vượt quá 150 ký tự.")]
    public string Name { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}

// --- Blog DTOs ---

public class BlogDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Summary { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int Views { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BlogDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? Summary { get; set; }
    public string? ThumbnailUrl { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int Views { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateBlogDto
{
    [Required(ErrorMessage = "Tiêu đề bài viết không được để trống.")]
    [MaxLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự.")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Nội dung bài viết không được để trống.")]
    public string Body { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự.")]
    public string? Summary { get; set; }

    public string? ThumbnailUrl { get; set; }

    [Required(ErrorMessage = "Danh mục bài viết không được để trống.")]
    public Guid CategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateBlogDto
{
    [Required(ErrorMessage = "Tiêu đề bài viết không được để trống.")]
    [MaxLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự.")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Nội dung bài viết không được để trống.")]
    public string Body { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự.")]
    public string? Summary { get; set; }

    public string? ThumbnailUrl { get; set; }

    [Required(ErrorMessage = "Danh mục bài viết không được để trống.")]
    public Guid CategoryId { get; set; }

    public bool IsActive { get; set; }
}
