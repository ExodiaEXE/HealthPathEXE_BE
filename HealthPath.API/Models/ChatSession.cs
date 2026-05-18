using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class ChatSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CompanionId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual AiCompanion Companion { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
