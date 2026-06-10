using System;

namespace HealthPath.API.Models;

public class CompanionMissionProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TemplateId { get; set; }
    public string? DateKey { get; set; }
    public int Progress { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual CompanionMissionTemplate Template { get; set; } = null!;
}
