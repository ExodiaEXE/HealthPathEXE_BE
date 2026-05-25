using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;
using HealthPath.API.Options;

namespace HealthPath.API.Services;

public class CloudflareR2Service : IFileStorageService
{
    private readonly CloudflareR2Options _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CloudflareR2Service> _logger;
    private readonly bool _isLocalFallback;

    public CloudflareR2Service(
        IOptions<CloudflareR2Options> options,
        IHostEnvironment environment,
        ILogger<CloudflareR2Service> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;

        // Check if configuration is a placeholder or incomplete
        _isLocalFallback = string.IsNullOrEmpty(_options.AccountId) ||
                           _options.AccountId.StartsWith("your-") ||
                           string.IsNullOrEmpty(_options.AccessKeyId) ||
                           string.IsNullOrEmpty(_options.SecretAccessKey);

        if (_isLocalFallback)
        {
            _logger.LogWarning("Cloudflare R2 credentials are not configured or are placeholders. Falling back to local filesystem storage.");
        }
    }

    private AmazonS3Client CreateS3Client()
    {
        if (_isLocalFallback)
        {
            throw new InvalidOperationException("Cannot create S3 client in local fallback mode.");
        }

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true
        };

        return new AmazonS3Client(_options.AccessKeyId, _options.SecretAccessKey, config);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var fileKey = string.IsNullOrEmpty(folder) ? uniqueFileName : $"{folder.Trim('/')}/{uniqueFileName}";

        if (_isLocalFallback)
        {
            // Save locally to wwwroot/uploads
            var contentRoot = _environment.ContentRootPath;
            var uploadsFolder = Path.Combine(contentRoot, "wwwroot", "uploads", folder);
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var destinationStream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(destinationStream);
            }

            _logger.LogInformation("Saved file locally to {FilePath}", filePath);
            return $"/uploads/{folder.Trim('/')}/{uniqueFileName}";
        }

        try
        {
            using var client = CreateS3Client();
            using var utility = new TransferUtility(client);

            var request = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileKey,
                BucketName = _options.BucketName,
                ContentType = contentType,
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true
            };

            await utility.UploadAsync(request);

            var domain = _options.PublicDomain.TrimEnd('/');
            if (!domain.StartsWith("http://") && !domain.StartsWith("https://"))
            {
                domain = "https://" + domain;
            }

            _logger.LogInformation("Uploaded file to Cloudflare R2 with key: {Key}", fileKey);
            return $"{domain}/{fileKey}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Cloudflare R2 with key: {Key}", fileKey);
            throw;
        }
    }

    public async Task DeleteAsync(string fileUrlOrKey)
    {
        if (string.IsNullOrEmpty(fileUrlOrKey)) return;

        // Extract key from URL if it is a URL
        string key = fileUrlOrKey;
        if (fileUrlOrKey.StartsWith("http://") || fileUrlOrKey.StartsWith("https://"))
        {
            var uri = new Uri(fileUrlOrKey);
            key = uri.AbsolutePath.TrimStart('/');
        }

        if (_isLocalFallback)
        {
            // Delete locally
            var contentRoot = _environment.ContentRootPath;
            var filePath = Path.Combine(contentRoot, "wwwroot", fileUrlOrKey.TrimStart('/'));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted local file at {FilePath}", filePath);
            }
            return;
        }

        try
        {
            using var client = CreateS3Client();
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            await client.DeleteObjectAsync(deleteRequest);
            _logger.LogInformation("Deleted file from Cloudflare R2 with key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from Cloudflare R2 with key: {Key}", key);
        }
    }

    public Task<string> GeneratePresignedUploadUrlAsync(string fileKey, string contentType, int expiresInMinutes = 15)
    {
        if (_isLocalFallback)
        {
            // Mock URL
            return Task.FromResult($"/uploads/mock-presigned?key={fileKey}");
        }

        try
        {
            using var client = CreateS3Client();
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = fileKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
                ContentType = contentType
            };

            var url = client.GetPreSignedURL(request);
            _logger.LogInformation("Generated pre-signed upload URL for key: {Key}", fileKey);
            return Task.FromResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate pre-signed upload URL for key: {Key}", fileKey);
            throw;
        }
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(string fileKey, int expiresInMinutes = 60)
    {
        if (_isLocalFallback)
        {
            // Trả về URL cục bộ để phát nhạc offline khi chạy local fallback
            var cleanKey = fileKey.TrimStart('/');
            if (cleanKey.StartsWith("uploads/"))
            {
                return Task.FromResult($"/{cleanKey}");
            }
            return Task.FromResult($"/uploads/{cleanKey}");
        }

        try
        {
            using var client = CreateS3Client();
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = fileKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes)
            };

            var url = client.GetPreSignedURL(request);
            _logger.LogInformation("Generated pre-signed download URL for key: {Key}", fileKey);
            return Task.FromResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate pre-signed download URL for key: {Key}", fileKey);
            throw;
        }
    }
}
