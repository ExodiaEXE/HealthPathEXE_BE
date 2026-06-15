using System;

namespace HealthPath.API.Models;

public partial class Blog
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string? Summary { get; set; }

    public string? ThumbnailUrl { get; set; }

    public Guid CategoryId { get; set; }

    public bool IsActive { get; set; } = true;

    public int Views { get; set; } = 0;

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }

    public virtual BlogCategory Category { get; set; } = null!;

    public virtual Admin? CreatedByNavigation { get; set; }
}
