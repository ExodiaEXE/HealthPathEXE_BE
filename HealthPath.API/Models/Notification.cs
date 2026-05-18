using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string Data { get; set; } = null!;

    public string Channel { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime SentAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
