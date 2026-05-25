using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthPath.API.Common;
using HealthPath.API.Models.DTOs;

namespace HealthPath.API.Services;

public interface IAudioTrackService
{
    // --- Browse (All users) ---
    Task<ApiResponse<PageResponse<AudioTrackDto>>> GetTracksAsync(
        string? category, string? search, bool? isPremium,
        string sortBy, int page, int pageSize, Guid? currentUserId);
    
    Task<ApiResponse<AudioTrackDetailDto>> GetTrackByIdAsync(Guid trackId, Guid? currentUserId);

    // --- Streaming (Presigned URL) ---
    Task<ApiResponse<AudioStreamUrlDto>> GetStreamUrlAsync(Guid trackId, Guid userId);

    // --- CRUD (Admin only) ---
    Task<ApiResponse<AudioTrackDto>> CreateTrackAsync(CreateAudioTrackDto dto, Guid adminUserId);
    Task<ApiResponse<AudioTrackDto>> UpdateTrackAsync(Guid trackId, UpdateAudioTrackDto dto, Guid adminUserId);
    Task<ApiResponse<object>> DeleteTrackAsync(Guid trackId, Guid adminUserId);

    // --- Categories (Admin CRUD + User Read) ---
    Task<ApiResponse<List<AudioCategoryDto>>> GetCategoriesAsync();
    Task<ApiResponse<List<AudioCategoryDto>>> GetAllCategoriesForAdminAsync(Guid adminUserId);
    Task<ApiResponse<AudioCategoryDto>> GetCategoryByIdAsync(Guid categoryId);
    Task<ApiResponse<AudioCategoryDto>> CreateCategoryAsync(CreateAudioCategoryDto dto, Guid adminUserId);
    Task<ApiResponse<AudioCategoryDto>> UpdateCategoryAsync(Guid categoryId, UpdateAudioCategoryDto dto, Guid adminUserId);
    Task<ApiResponse<object>> DeleteCategoryAsync(Guid categoryId, Guid adminUserId);

    // --- Play History ---
    Task<ApiResponse<object>> RecordPlayAsync(RecordPlayDto dto, Guid userId);
    Task<ApiResponse<PageResponse<AudioHistoryDto>>> GetPlayHistoryAsync(Guid userId, int page, int pageSize);
    Task<ApiResponse<AudioStatsDto>> GetListeningStatsAsync(Guid userId);

    // --- Favorites ---
    Task<ApiResponse<object>> AddFavoriteAsync(Guid trackId, Guid userId);
    Task<ApiResponse<object>> RemoveFavoriteAsync(Guid trackId, Guid userId);
    Task<ApiResponse<PageResponse<AudioTrackDto>>> GetFavoritesAsync(Guid userId, int page, int pageSize);
}
