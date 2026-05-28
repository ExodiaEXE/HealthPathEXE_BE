using System;

namespace HealthPath.API.Models.DTOs
{
    public class CreateGroupDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class UpdateGroupDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class GroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
    }
}