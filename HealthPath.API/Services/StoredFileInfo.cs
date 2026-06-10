using System;

namespace HealthPath.API.Services;

public class StoredFileInfo
{
    public string FileKey { get; set; } = null!;
    public string Url { get; set; } = null!;
    public long SizeBytes { get; set; }
    public DateTime? LastModified { get; set; }
}
