namespace HealthPath.API.Options;

public class CloudflareR2Options
{
    public string AccountId { get; set; } = null!;
    public string AccessKeyId { get; set; } = null!;
    public string SecretAccessKey { get; set; } = null!;
    public string BucketName { get; set; } = null!;
    public string PublicDomain { get; set; } = null!;
}
