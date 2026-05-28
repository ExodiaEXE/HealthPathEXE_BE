using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthPath.API.Services
{
    public interface IGroupService
    {
        Task<ApiResponse<GroupDto>> CreateGroupAsync(Guid userId, CreateGroupDto dto);
        Task<ApiResponse<List<GroupDto>>> GetMyGroupsAsync(Guid userId);
        Task<ApiResponse<GroupDto>> GetByIdAsync(Guid id, Guid userId);
        Task<ApiResponse<GroupDto>> UpdateGroupAsync(Guid id, Guid userId, UpdateGroupDto dto);
        Task<ApiResponse<object>> DeleteGroupAsync(Guid id, Guid userId);
        Task<ApiResponse<object>> JoinGroupAsync(Guid id, Guid userId);
    }
}