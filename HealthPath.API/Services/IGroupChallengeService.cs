using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services
{
    public interface IGroupChallengeService
    {
        Task<GroupChallenge> CreateChallengeAsync(CreateGroupChallengeDto dto);
        Task<IEnumerable<GroupChallenge>> GetChallengesByGroupAsync(Guid groupId);
    }
}