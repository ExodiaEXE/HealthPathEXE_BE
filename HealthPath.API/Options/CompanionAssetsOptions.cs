namespace HealthPath.API.Options;

/// <summary>
/// CDN URLs for companion 3D assets (GLB). Replace MascotGlbUrl when custom Mèo Xanh is on R2.
/// </summary>
public class CompanionAssetsOptions
{
    public const string SectionName = "CompanionAssets";

    public string Version { get; set; } = "1";
    public bool Enable3D { get; set; } = true;

    /// <summary>Primary mascot GLB (Draco-compressed, &lt; 2 MB recommended).</summary>
    public string MascotGlbUrl { get; set; } = string.Empty;

    public string? RoomCozyGlbUrl { get; set; }
    public string? RoomModernGlbUrl { get; set; }
    public string? RoomNatureGlbUrl { get; set; }

    public string AnimationIdle { get; set; } = "Survey";
    public string AnimationHappy { get; set; } = "Run";
    public string AnimationEat { get; set; } = "Walk";
    public string AnimationSad { get; set; } = "Survey";
    public string AnimationSleepy { get; set; } = "Survey";
    public string AnimationWave { get; set; } = "Run";
    public string AnimationHungry { get; set; } = "Survey";
}
