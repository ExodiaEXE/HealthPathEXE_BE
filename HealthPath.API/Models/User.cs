using System;
using System.Collections.Generic;

namespace HealthPath.API.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? OtpCode { get; set; }

    public DateTime? OtpExpiryTime { get; set; }

    public string? GoogleId { get; set; }

    public string? FacebookId { get; set; }

    public virtual ICollection<AudioTrack> AudioTracks { get; set; } = new List<AudioTrack>();

    public virtual ICollection<ChallengeParticipant> ChallengeParticipants { get; set; } = new List<ChallengeParticipant>();

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<MoodCheckin> MoodCheckins { get; set; } = new List<MoodCheckin>();

    public virtual NotificationSetting? NotificationSetting { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Routine> Routines { get; set; } = new List<Routine>();

    public virtual ICollection<UserAudioHistory> UserAudioHistories { get; set; } = new List<UserAudioHistory>();

    public virtual ICollection<UserRole> UserRoleAssignedByNavigations { get; set; } = new List<UserRole>();

    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();

    public virtual ICollection<UserRoutine> UserRoutines { get; set; } = new List<UserRoutine>();

    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

    public virtual ICollection<DeviceToken> DeviceTokens { get; set; } = new List<DeviceToken>();

    public virtual ICollection<UserFavoriteTrack> FavoriteTracks { get; set; } = new List<UserFavoriteTrack>();

    public virtual UserCompanion? UserCompanion { get; set; }

    public virtual ICollection<CompanionInventory> CompanionInventories { get; set; } = new List<CompanionInventory>();

    public virtual ICollection<CompanionMissionProgress> CompanionMissionProgresses { get; set; } = new List<CompanionMissionProgress>();
}
