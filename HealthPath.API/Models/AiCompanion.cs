using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class AiCompanion
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string PersonaPrompt { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}
