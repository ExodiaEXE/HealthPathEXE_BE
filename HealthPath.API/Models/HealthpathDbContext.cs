using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HealthPath.API.Models;

public partial class HealthpathDbContext : DbContext
{
    public HealthpathDbContext()
    {
    }

    public HealthpathDbContext(DbContextOptions<HealthpathDbContext> options)
        : base(options)
    {
    }


    public virtual DbSet<AiCompanion> AiCompanions { get; set; } = null!;
    public virtual DbSet<AudioTrack> AudioTracks { get; set; } = null!;
    public virtual DbSet<ChallengeParticipant> ChallengeParticipants { get; set; } = null!;
    public virtual DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public virtual DbSet<ChatSession> ChatSessions { get; set; } = null!;
    public virtual DbSet<Group> Groups { get; set; } = null!;
    public virtual DbSet<GroupChallenge> GroupChallenges { get; set; } = null!;
    public virtual DbSet<GroupMember> GroupMembers { get; set; } = null!;
    public virtual DbSet<MoodCheckin> MoodCheckins { get; set; } = null!;
    public virtual DbSet<Notification> Notifications { get; set; } = null!;
    public virtual DbSet<NotificationSetting> NotificationSettings { get; set; } = null!;
    public virtual DbSet<Permission> Permissions { get; set; } = null!;
    public virtual DbSet<Role> Roles { get; set; } = null!;
    public virtual DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public virtual DbSet<Routine> Routines { get; set; } = null!;
    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<UserAudioHistory> UserAudioHistories { get; set; } = null!;
    public virtual DbSet<UserRole> UserRoles { get; set; } = null!;
    public virtual DbSet<UserRoutine> UserRoutines { get; set; } = null!;
    public virtual DbSet<UserSubscription> UserSubscriptions { get; set; } = null!;
    public virtual DbSet<UserStats> UserStats { get; set; } = null!;
    public virtual DbSet<RecurringTemplate> RecurringTemplates { get; set; } = null!;
    public virtual DbSet<DeviceToken> DeviceTokens { get; set; } = null!;

    public virtual DbSet<AudioCategory> AudioCategories { get; set; } = null!;
    public virtual DbSet<UserFavoriteTrack> UserFavoriteTracks { get; set; } = null!;

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    => optionsBuilder.UseNpgsql("Host=localhost;Database=healthpath_db;Username=postgres;Password=1234567890");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("pg_trgm")
            .HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<AiCompanion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ai_companions_pkey");

            entity.ToTable("ai_companions");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PersonaPrompt)
                .HasDefaultValueSql("''::text")
                .HasColumnName("persona_prompt");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<AudioTrack>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audio_tracks_pkey");

            entity.ToTable("audio_tracks");

            entity.HasIndex(e => e.CategoryId, "idx_audio_category").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Artist)
                .HasMaxLength(150)
                .HasColumnName("artist");
            entity.Property(e => e.CategoryId)
                .HasColumnName("category_id");
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(e => e.FileUrl).HasColumnName("file_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsPremium).HasColumnName("is_premium");
            entity.Property(e => e.PlayCount).HasColumnName("play_count");
            entity.Property(e => e.Studio)
                .HasMaxLength(150)
                .HasColumnName("studio");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.Category).WithMany(p => p.AudioTracks)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("audio_tracks_category_id_fkey");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.AudioTracks)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("audio_tracks_uploaded_by_fkey");
        });

        modelBuilder.Entity<ChallengeParticipant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("challenge_participants_pkey");

            entity.ToTable("challenge_participants");

            entity.HasIndex(e => new { e.ChallengeId, e.UserId }, "challenge_participants_challenge_id_user_id_key").IsUnique();

            entity.HasIndex(e => e.UserId, "idx_challenge_part_user").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChallengeId).HasColumnName("challenge_id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("joined_at");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'active'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Challenge).WithMany(p => p.ChallengeParticipants)
                .HasForeignKey(d => d.ChallengeId)
                .HasConstraintName("challenge_participants_challenge_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ChallengeParticipants)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("challenge_participants_user_id_fkey");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("chat_messages_pkey");

            entity.ToTable("chat_messages");

            entity.HasIndex(e => e.SentAt, "idx_chat_messages_sent")
                .IsDescending()
                .HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.SessionId, "idx_chat_messages_sess").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.MessageType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'text'::character varying")
                .HasColumnName("message_type");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("sent_at");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.Session).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("chat_messages_session_id_fkey");
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("chat_sessions_pkey");

            entity.ToTable("chat_sessions");

            entity.HasIndex(e => e.UserId, "idx_chat_sessions_user").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompanionId).HasColumnName("companion_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.EndedAt).HasColumnName("ended_at");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'active'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Companion).WithMany(p => p.ChatSessions)
                .HasForeignKey(d => d.CompanionId)
                .HasConstraintName("chat_sessions_companion_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ChatSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("chat_sessions_user_id_fkey");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("groups_pkey");

            entity.ToTable("groups");

            entity.HasIndex(e => e.InviteCode, "groups_invite_code_key").IsUnique();

            entity.HasIndex(e => e.OwnerId, "idx_groups_owner").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CoverUrl).HasColumnName("cover_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.InviteCode)
                .HasMaxLength(20)
                .HasDefaultValueSql("upper(SUBSTRING((gen_random_uuid())::text FROM 1 FOR 8))")
                .HasColumnName("invite_code");
            entity.Property(e => e.IsPublic)
                .HasDefaultValue(true)
                .HasColumnName("is_public");
            entity.Property(e => e.MaxMembers)
                .HasDefaultValue(50)
                .HasColumnName("max_members");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Owner).WithMany(p => p.Groups)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("groups_owner_id_fkey");
        });

        modelBuilder.Entity<GroupChallenge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_challenges_pkey");

            entity.ToTable("group_challenges");

            entity.HasIndex(e => e.GroupId, "idx_challenges_group").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndsAt).HasColumnName("ends_at");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.StartsAt).HasColumnName("starts_at");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupChallenges)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("group_challenges_group_id_fkey");
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("group_members_pkey");

            entity.ToTable("group_members");

            entity.HasIndex(e => new { e.GroupId, e.UserId }, "group_members_group_id_user_id_key").IsUnique();

            entity.HasIndex(e => e.GroupId, "idx_group_members_group").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.UserId, "idx_group_members_user").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("joined_at");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValueSql("'member'::character varying")
                .HasColumnName("role");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("group_members_group_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.GroupMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("group_members_user_id_fkey");
        });

        modelBuilder.Entity<MoodCheckin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mood_checkins_pkey");

            entity.ToTable("mood_checkins");

            entity.HasIndex(e => e.CheckedAt, "idx_mood_checked_at")
                .IsDescending()
                .HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.UserId, "idx_mood_user").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CheckedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("checked_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.EnergyLevel)
                .HasMaxLength(30)
                .HasColumnName("energy_level");
            entity.Property(e => e.Mood)
                .HasMaxLength(30)
                .HasColumnName("mood");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.StreakDay)
                .HasDefaultValue(1)
                .HasColumnName("streak_day");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.MoodCheckins)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("mood_checkins_user_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.SentAt, "idx_notifs_sent_at")
                .IsDescending()
                .HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => new { e.UserId, e.IsRead }, "idx_notifs_user_unread").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.Channel)
                .HasMaxLength(20)
                .HasDefaultValueSql("'in_app'::character varying")
                .HasColumnName("channel");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Data)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("data");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("sent_at");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(60)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<NotificationSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notification_settings_pkey");

            entity.ToTable("notification_settings");

            entity.HasIndex(e => e.UserId, "notification_settings_user_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChallengeUpdates)
                .HasDefaultValue(true)
                .HasColumnName("challenge_updates");
            entity.Property(e => e.DailyCheckin)
                .HasDefaultValue(true)
                .HasColumnName("daily_checkin");
            entity.Property(e => e.EmailEnabled)
                .HasDefaultValue(true)
                .HasColumnName("email_enabled");
            entity.Property(e => e.GroupActivity)
                .HasDefaultValue(true)
                .HasColumnName("group_activity");
            entity.Property(e => e.InAppEnabled)
                .HasDefaultValue(true)
                .HasColumnName("in_app_enabled");
            entity.Property(e => e.Promotions).HasColumnName("promotions");
            entity.Property(e => e.PushEnabled)
                .HasDefaultValue(true)
                .HasColumnName("push_enabled");
            entity.Property(e => e.QuietFrom)
                .HasDefaultValueSql("'22:00:00'::time without time zone")
                .HasColumnName("quiet_from");
            entity.Property(e => e.QuietUntil)
                .HasDefaultValueSql("'07:00:00'::time without time zone")
                .HasColumnName("quiet_until");
            entity.Property(e => e.StreakReminder)
                .HasDefaultValue(true)
                .HasColumnName("streak_reminder");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.NotificationSetting)
                .HasForeignKey<NotificationSetting>(d => d.UserId)
                .HasConstraintName("notification_settings_user_id_fkey");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("permissions_pkey");

            entity.ToTable("permissions");

            entity.HasIndex(e => new { e.Resource, e.Action }, "permissions_resource_action_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(80)
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Resource)
                .HasMaxLength(80)
                .HasColumnName("resource");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsSystem).HasColumnName("is_system");
            entity.Property(e => e.Name)
                .HasMaxLength(80)
                .HasColumnName("name");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_permissions_pkey");

            entity.ToTable("role_permissions");

            entity.HasIndex(e => new { e.RoleId, e.PermissionId }, "role_permissions_role_id_permission_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .HasConstraintName("role_permissions_permission_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("role_permissions_role_id_fkey");
        });

        modelBuilder.Entity<Routine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("routines_pkey");

            entity.ToTable("routines");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Difficulty)
                .HasMaxLength(20)
                .HasDefaultValueSql("'easy'::character varying")
                .HasColumnName("difficulty");
            entity.Property(e => e.DurationMinutes)
                .HasDefaultValue(10)
                .HasColumnName("duration_minutes");
            entity.Property(e => e.IsPremium).HasColumnName("is_premium");
            entity.Property(e => e.IsSystem).HasColumnName("is_system");
            entity.Property(e => e.ThumbnailUrl).HasColumnName("thumbnail_url");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Routines)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("routines_created_by_fkey");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subscription_plans_pkey");

            entity.ToTable("subscription_plans");

            entity.HasIndex(e => e.Code, "subscription_plans_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValueSql("'VND'::bpchar")
                .IsFixedLength()
                .HasColumnName("currency");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Features)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("features");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PriceMonthly)
                .HasPrecision(12, 2)
                .HasColumnName("price_monthly");
            entity.Property(e => e.PriceYearly)
                .HasPrecision(12, 2)
                .HasColumnName("price_yearly");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.IsActive, "idx_users_active").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.Email, "idx_users_email").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmailVerifiedAt).HasColumnName("email_verified_at");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsVerified).HasColumnName("is_verified");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<UserAudioHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_audio_history_pkey");

            entity.ToTable("user_audio_history");

            entity.HasIndex(e => e.TrackId, "idx_audio_history_track").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.UserId, "idx_audio_history_user").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.PlayedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("played_at");
            entity.Property(e => e.PlayedSeconds).HasColumnName("played_seconds");
            entity.Property(e => e.TrackId).HasColumnName("track_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Track).WithMany(p => p.UserAudioHistories)
                .HasForeignKey(d => d.TrackId)
                .HasConstraintName("user_audio_history_track_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserAudioHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_audio_history_user_id_fkey");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_roles_pkey");

            entity.ToTable("user_roles");

            entity.HasIndex(e => e.RoleId, "idx_user_roles_role").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.UserId, "idx_user_roles_user").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => new { e.UserId, e.RoleId }, "user_roles_user_id_role_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.UserRoleAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("user_roles_assigned_by_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("user_roles_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoleUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_roles_user_id_fkey");
        });

        modelBuilder.Entity<UserRoutine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_routines_pkey");

            entity.ToTable("user_routines");

            entity.HasIndex(e => e.ScheduledAt, "idx_user_routines_sched").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.UserId, "idx_user_routines_user").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.RoutineId).HasColumnName("routine_id");
            entity.Property(e => e.ScheduledAt).HasColumnName("scheduled_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.ActualDurationMinutes).HasColumnName("actual_duration_minutes");

            entity.Property(e => e.ElapsedSeconds)
                .HasDefaultValue(0)
                .HasColumnName("elapsed_seconds");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Routine).WithMany(p => p.UserRoutines)
                .HasForeignKey(d => d.RoutineId)
                .HasConstraintName("user_routines_routine_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoutines)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_routines_user_id_fkey");
        });

        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_subscriptions_pkey");

            entity.ToTable("user_subscriptions");

            entity.HasIndex(e => e.ExpiresAt, "idx_user_subs_expires").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.Status, "idx_user_subs_status").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.UserId, "idx_user_subs_user").HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BillingCycle)
                .HasMaxLength(20)
                .HasDefaultValueSql("'monthly'::character varying")
                .HasColumnName("billing_cycle");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.PaymentProvider)
                .HasMaxLength(50)
                .HasColumnName("payment_provider");
            entity.Property(e => e.PaymentRef)
                .HasMaxLength(255)
                .HasColumnName("payment_ref");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'active'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Plan).WithMany(p => p.UserSubscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_subscriptions_plan_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserSubscriptions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_subscriptions_user_id_fkey");
        });

        modelBuilder.Entity<UserStats>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_stats_pkey");
            entity.ToTable("user_stats");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.StreakCurrent)
                .HasDefaultValue(0)
                .HasColumnName("streak_current");
            entity.Property(e => e.StreakBest)
                .HasDefaultValue(0)
                .HasColumnName("streak_best");
            entity.Property(e => e.StreakUpdatedDate).HasColumnName("streak_updated_date");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithMany() // User has no UserStats navigation property right now, just keep it one-sided
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_stats_user_id_fkey");
        });

        modelBuilder.Entity<RecurringTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recurring_templates_pkey");
            entity.ToTable("recurring_templates");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoutineId).HasColumnName("routine_id");
            entity.Property(e => e.DaysOfWeek)
                .HasColumnType("jsonb")
                .HasColumnName("days_of_week");
            entity.Property(e => e.ScheduledTime).HasColumnName("scheduled_time");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("recurring_templates_user_id_fkey");

            entity.HasOne(d => d.Routine).WithMany()
                .HasForeignKey(d => d.RoutineId)
                .HasConstraintName("recurring_templates_routine_id_fkey");
        });

        modelBuilder.Entity<DeviceToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("device_tokens_pkey");
            entity.ToTable("device_tokens");

            entity.HasIndex(e => new { e.UserId, e.Token }, "device_tokens_user_id_token_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.Platform)
                .HasMaxLength(20)
                .HasColumnName("platform");
            entity.Property(e => e.DeviceName)
                .HasMaxLength(100)
                .HasColumnName("device_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithMany(p => p.DeviceTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("device_tokens_user_id_fkey");
        });

        modelBuilder.Entity<AudioCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audio_categories_pkey");

            entity.ToTable("audio_categories");

            entity.HasIndex(e => e.Name, "idx_audio_category_name_unique").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.IconUrl)
                .HasColumnName("icon_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<UserFavoriteTrack>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_favorite_tracks_pkey");

            entity.ToTable("user_favorite_tracks");

            entity.HasIndex(e => new { e.UserId, e.TrackId }, "idx_user_favorite_track_unique").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TrackId).HasColumnName("track_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.User).WithMany(p => p.FavoriteTracks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_favorite_tracks_user_id_fkey");

            entity.HasOne(d => d.Track).WithMany(p => p.FavoritedByUsers)
                .HasForeignKey(d => d.TrackId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_favorite_tracks_track_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}