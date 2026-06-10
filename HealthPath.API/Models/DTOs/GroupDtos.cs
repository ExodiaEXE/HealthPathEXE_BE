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
        public string InviteCode { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
    }

    public class JoinGroupByInviteCodeDto
    {
        public string InviteCode { get; set; } = null!;
    }

    public class GroupMemberDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsCurrentUser { get; set; }
        public int WeeklyScore { get; set; }
    }

    public class LeaveGroupResultDto
    {
        public bool GroupDeleted { get; set; }
    }
}