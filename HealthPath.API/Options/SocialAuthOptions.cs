namespace HealthPath.API.Options;

/// <summary>
/// Xác minh token Google/Facebook từ mobile. Secret OAuth nằm ở client;
/// backend chỉ cần Client ID (Google aud) và App Secret (Facebook debug_token).
/// </summary>
public class SocialAuthOptions
{
    public const string SectionName = "SocialAuth";

    /// <summary>Development: cho phép token mock_google_token_* / mock_facebook_token_*.</summary>
    public bool AllowMockTokens { get; set; } = true;

    /// <summary>Google OAuth Client IDs (Web + Android), phân tách bằng dấu phẩy — khớp claim aud.</summary>
    public string GoogleClientIds { get; set; } = string.Empty;

    public string FacebookAppId { get; set; } = string.Empty;

    /// <summary>App Secret — dùng debug_token (khuyến nghị production).</summary>
    public string FacebookAppSecret { get; set; } = string.Empty;
}
