using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthPath.Tests.Services;

public class BlogServiceTests
{
    private readonly Mock<ILogger<BlogService>> _mockLogger;

    public BlogServiceTests()
    {
        _mockLogger = new Mock<ILogger<BlogService>>();
    }

    [Fact]
    public void SlugHelper_GenerateSlug_ConvertsVietnameseCorrectly()
    {
        // Arrange
        string phrase = "Chế độ Ăn 123! @# Lành Mạnh";

        // Act
        string slug = SlugHelper.GenerateSlug(phrase);

        // Assert
        slug.Should().Be("che-do-an-123-lanh-manh");
    }

    [Fact]
    public async Task BlogService_CreateCategory_GeneratesUniqueSlug()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var service = new BlogService(context, _mockLogger.Object);

        var dto1 = new CreateBlogCategoryDto { Name = "Dinh Dưỡng", Description = "Desc" };
        var dto2 = new CreateBlogCategoryDto { Name = "Dinh Dưỡng", Description = "Desc 2" };

        // Act
        var res1 = await service.CreateCategoryAsync(dto1);
        var res2 = await service.CreateCategoryAsync(dto2);

        // Assert
        res1.Success.Should().BeTrue();
        res1.Data!.Slug.Should().Be("dinh-duong");

        res2.Success.Should().BeFalse(); // Tên trùng bị chặn bởi VALIDATION / NAME_TAKEN
        res2.ErrorCode.Should().Be(ErrorCode.BLOG_CATEGORY_NAME_TAKEN.ToString());
    }

    [Fact]
    public async Task BlogService_CreateBlog_GeneratesIncrementalSlugOnDuplicateTitle()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var service = new BlogService(context, _mockLogger.Object);

        // 1. Tạo Category
        var category = new BlogCategory { Id = Guid.NewGuid(), Name = "Sức Khỏe", Slug = "suc-khoe" };
        context.BlogCategories.Add(category);
        await context.SaveChangesAsync();

        var dto1 = new CreateBlogDto { Title = "Bài viết số một", Body = "Body", CategoryId = category.Id };
        var dto2 = new CreateBlogDto { Title = "Bài viết số một", Body = "Body 2", CategoryId = category.Id };

        // Act
        var res1 = await service.CreateBlogAsync(dto1, Guid.NewGuid());
        var res2 = await service.CreateBlogAsync(dto2, Guid.NewGuid());

        // Assert
        res1.Success.Should().BeTrue();
        res1.Data!.Slug.Should().Be("bai-viet-so-mot");

        res2.Success.Should().BeTrue();
        res2.Data!.Slug.Should().Be("bai-viet-so-mot-1"); // Sinh slug tăng tiến để tránh trùng unique constraint
    }

    [Fact]
    public async Task BlogService_GetBlogBySlug_IncreasesViewsCount()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var service = new BlogService(context, _mockLogger.Object);

        var category = new BlogCategory { Id = Guid.NewGuid(), Name = "Sức Khỏe", Slug = "suc-khoe", IsActive = true };
        context.BlogCategories.Add(category);

        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = "Cách thiền định",
            Slug = "cach-thien-dinh",
            Body = "Nội dung thiền",
            CategoryId = category.Id,
            IsActive = true,
            Views = 10
        };
        context.Blogs.Add(blog);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetBlogBySlugAsync("cach-thien-dinh");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Views.Should().Be(11); // Tăng lên 11

        var dbBlog = context.Blogs.First(b => b.Id == blog.Id);
        dbBlog.Views.Should().Be(11);
    }

    [Fact]
    public async Task BlogService_DeleteCategory_WithActiveBlogs_ReturnsFail()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var service = new BlogService(context, _mockLogger.Object);

        var category = new BlogCategory { Id = Guid.NewGuid(), Name = "Danh mục có bài viết", Slug = "dm-co-bai-viet" };
        context.BlogCategories.Add(category);

        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết",
            Slug = "bai-viet",
            Body = "Body",
            CategoryId = category.Id
        };
        context.Blogs.Add(blog);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteCategoryAsync(category.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Không thể xóa danh mục đang có chứa bài viết");
    }
}
