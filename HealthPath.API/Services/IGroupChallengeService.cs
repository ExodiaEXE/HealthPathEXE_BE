using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthPath.API.Services
{
    public interface IGroupChallengeService
    {
        Task<GroupChallenge> CreateChallengeAsync(CreateGroupChallengeDto dto);
        Task<IEnumerable<GroupChallenge>> GetChallengesByGroupAsync(Guid groupId);
        Task<GroupChallenge?> GetChallengeByIdAsync(Guid id);
        Task<GroupChallenge> UpdateChallengeAsync(Guid id, UpdateGroupChallengeDto dto);
        Task<bool> DeleteChallengeAsync(Guid id);
    }
}