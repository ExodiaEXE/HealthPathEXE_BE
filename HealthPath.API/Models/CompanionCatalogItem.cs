using System;

namespace HealthPath.API.Models;

public class CompanionCatalogItem
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "furniture";
    public int Price { get; set; }
    public string IconEmoji { get; set; } = "🪴";
    public string? PreviewUrl { get; set; }
    public bool IsDefaultOwned { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
