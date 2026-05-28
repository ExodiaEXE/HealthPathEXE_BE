using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.Tests.Helpers;
using Xunit;

namespace HealthPath.Tests.Services;

public class GroupAndChallengeServiceTests
{
    private static (User owner, User member, HealthpathDbContext context) SeedUsers()
    {
        var context = DbContextFactory.Create();
        var owner = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Group Owner",
            Email = $"owner-{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash",
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var member = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Group Member",
            Email = $"member-{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash",
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.AddRange(owner, member);
        context.SaveChanges();
        return (owner, member, context);
    }

    [Fact]
    public async Task Group_FullCrudAndJoinFlow_Succeeds()
    {
        var (owner, member, context) = SeedUsers();
        var groupService = new GroupService(context);

        var create = await groupService.CreateGroupAsync(owner.Id, new CreateGroupDto
        {
            Name = "Test Group",
            Description = "Desc"
        });
        create.Success.Should().BeTrue();
        var groupId = create.Data!.Id;

        var myGroups = await groupService.GetMyGroupsAsync(owner.Id);
        myGroups.Success.Should().BeTrue();
        myGroups.Data!.Should().ContainSingle(g => g.Id == groupId);

        var getById = await groupService.GetByIdAsync(groupId, owner.Id);
        getById.Success.Should().BeTrue();
        getById.Data!.Name.Should().Be("Test Group");

        var update = await groupService.UpdateGroupAsync(groupId, owner.Id, new UpdateGroupDto
        {
            Name = "Updated Group",
            Description = "New desc"
        });
        update.Success.Should().BeTrue();
        update.Data!.Name.Should().Be("Updated Group");

        var join = await groupService.JoinGroupAsync(groupId, member.Id);
        join.Success.Should().BeTrue();

        var joinAgain = await groupService.JoinGroupAsync(groupId, member.Id);
        joinAgain.Success.Should().BeFalse();
        joinAgain.ErrorCode.Should().Be("ALREADY_MEMBER");

        var memberGroups = await groupService.GetMyGroupsAsync(member.Id);
        memberGroups.Data!.Should().Contain(g => g.Id == groupId);

        var delete = await groupService.DeleteGroupAsync(groupId, owner.Id);
        delete.Success.Should().BeTrue();

        var afterDelete = await groupService.GetByIdAsync(groupId, owner.Id);
        afterDelete.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Group_GetById_NotFound_ReturnsFail()
    {
        var (owner, _, context) = SeedUsers();
        var groupService = new GroupService(context);

        var result = await groupService.GetByIdAsync(Guid.NewGuid(), owner.Id);
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("GROUP_NOT_FOUND");
    }

    [Fact]
    public async Task GroupChallenge_FullCrudFlow_Succeeds()
    {
        var (owner, _, context) = SeedUsers();
        var groupService = new GroupService(context);
        var challengeService = new GroupChallengeService(context);

        var group = await groupService.CreateGroupAsync(owner.Id, new CreateGroupDto { Name = "Challenge Group" });
        var groupId = group.Data!.Id;

        var starts = DateTime.UtcNow.AddDays(1);
        var ends = DateTime.UtcNow.AddDays(7);

        var created = await challengeService.CreateChallengeAsync(new CreateGroupChallengeDto
        {
            GroupId = groupId,
            Title = "Weekly Steps",
            Description = "Walk 10k",
            StartsAt = starts,
            EndsAt = ends
        });
        created.Title.Should().Be("Weekly Steps");
        created.IsActive.Should().BeTrue();

        var list = await challengeService.GetChallengesByGroupAsync(groupId);
        list.Should().ContainSingle(c => c.Id == created.Id);

        var byId = await challengeService.GetChallengeByIdAsync(created.Id);
        byId.Should().NotBeNull();

        var updated = await challengeService.UpdateChallengeAsync(created.Id, new UpdateGroupChallengeDto
        {
            Title = "Updated Challenge",
            Description = "Updated",
            StartsAt = starts,
            EndsAt = ends.AddDays(1),
            IsActive = false
        });
        updated.Title.Should().Be("Updated Challenge");
        updated.IsActive.Should().BeFalse();

        var deleted = await challengeService.DeleteChallengeAsync(created.Id);
        deleted.Should().BeTrue();

        var afterDelete = await challengeService.GetChallengeByIdAsync(created.Id);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task GroupChallenge_Create_ForMissingGroup_Throws()
    {
        var (_, _, context) = SeedUsers();
        var challengeService = new GroupChallengeService(context);

        var act = () => challengeService.CreateChallengeAsync(new CreateGroupChallengeDto
        {
            GroupId = Guid.NewGuid(),
            Title = "X",
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(1)
        });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Group không tồn tại*");
    }
}
