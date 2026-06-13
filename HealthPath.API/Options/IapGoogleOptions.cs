namespace HealthPath.API.Options;

public class IapGoogleOptions
{
    public const string SectionName = "IAP:Google";

    /// <summary>Đường dẫn file JSON service account hoặc nội dung JSON (bắt đầu bằng '{').</summary>
    public string ServiceAccountKey { get; set; } = string.Empty;

    /// <summary>Package Android trên Play Console — phải khớp app mobile.</summary>
    public string PackageName { get; set; } = "com.exodiateam.healthpath";
}
