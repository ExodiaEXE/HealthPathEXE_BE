using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Options;
using HealthPath.API.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HealthPath.Tests.Services;

public class CloudflareR2ServiceTests
{
    private readonly Mock<IHostEnvironment> _mockEnv;
    private readonly Mock<ILogger<CloudflareR2Service>> _mockLogger;
    private readonly IOptions<CloudflareR2Options> _options;

    public CloudflareR2ServiceTests()
    {
        _mockEnv = new Mock<IHostEnvironment>();
        _mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

        _mockLogger = new Mock<ILogger<CloudflareR2Service>>();

        // Use placeholder credentials to trigger the local fallback filesystem mode
        var r2Options = new CloudflareR2Options
        {
            AccountId = "your-account-id",
            AccessKeyId = "placeholder",
            SecretAccessKey = "placeholder",
            BucketName = "healthpath-media",
            PublicDomain = "media.healthpath.vn"
        };
        _options = Microsoft.Extensions.Options.Options.Create(r2Options);
    }

    [Fact]
    public async Task UploadAsync_LocalFallback_SavesToLocalFolderAndReturnsPath()
    {
        // Arrange
        var service = new CloudflareR2Service(_options, _mockEnv.Object, _mockLogger.Object);
        var content = "dummy file content";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        var fileName = "avatar.png";
        var contentType = "image/png";
        var folder = "avatars/test-user";

        // Act
        var url = await service.UploadAsync(stream, fileName, contentType, folder);

        // Assert
        url.Should().StartWith($"/uploads/{folder}/");
        url.Should().EndWith(".png");

        // Verify the file was actually written to disk in the mock environment
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", url.TrimStart('/'));
        File.Exists(filePath).Should().BeTrue();

        // Cleanup
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task DeleteAsync_LocalFallback_DeletesLocalFileSuccessfully()
    {
        // Arrange
        var service = new CloudflareR2Service(_options, _mockEnv.Object, _mockLogger.Object);
        
        // Setup local file
        var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "test");
        if (!Directory.Exists(wwwrootPath))
        {
            Directory.CreateDirectory(wwwrootPath);
        }
        var filePath = Path.Combine(wwwrootPath, "temp.txt");
        await File.WriteAllTextAsync(filePath, "temp content");

        var fileUrl = "/uploads/test/temp.txt";
        File.Exists(filePath).Should().BeTrue();

        // Act
        await service.DeleteAsync(fileUrl);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task GeneratePresignedUploadUrlAsync_LocalFallback_ReturnsMockPresignedUrl()
    {
        // Arrange
        var service = new CloudflareR2Service(_options, _mockEnv.Object, _mockLogger.Object);
        var key = "avatars/user-123/avatar.png";

        // Act
        var url = await service.GeneratePresignedUploadUrlAsync(key, "image/png");

        // Assert
        url.Should().Be($"/uploads/mock-presigned?key={key}");
    }
}
