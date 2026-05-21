using System;

namespace HealthPath.API.Models.DTOs;

public class FileUploadResultDto
{
    public string Url { get; set; } = null!;
    public string FileKey { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
}
