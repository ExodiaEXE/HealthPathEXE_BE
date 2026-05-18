using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class ChatMessage
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string Role { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string MessageType { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ChatSession Session { get; set; } = null!;
}
