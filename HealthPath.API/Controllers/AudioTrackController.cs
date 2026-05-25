using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthPath.API.Common;
using HealthPath.API.Extensions;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;

namespace HealthPath.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AudioTrackController : ControllerBase
    {
        private readonly IAudioTrackService _audioTrackService;

        public AudioTrackController(IAudioTrackService audioTrackService)
        {
            _audioTrackService = audioTrackService;
        }

        // --- Helper for consistent responses ---
        private IActionResult HandleResponse<T>(ApiResponse<T> response)
        {
            if (response.Success)
            {
                return Ok(response);
            }

            if (response.ErrorCode == ErrorCode.AUDIO_TRACK_NOT_FOUND.ToString() ||
                response.ErrorCode == ErrorCode.AUDIO_CATEGORY_NOT_FOUND.ToString())
            {
                return NotFound(response);
            }

            if (response.ErrorCode == ErrorCode.FORBIDDEN.ToString())
            {
                return StatusCode(403, response);
            }

            return BadRequest(response);
        }

        // ==========================================
        // --- BROWSE & VIEW ENDPOINTS (User/Admin) ---
        // ==========================================

        /// <summary>
        /// Duyệt danh sách bài hát (hỗ trợ lọc theo danh mục, tìm kiếm, premium, sắp xếp và phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTracks(
            [FromQuery] string? category,
            [FromQuery] string? search,
            [FromQuery] bool? isPremium,
            [FromQuery] string sortBy = "newest",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();
            var response = await _audioTrackService.GetTracksAsync(category, search, isPremium, sortBy, page, pageSize, userId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Lấy chi tiết bài hát theo ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTrackById(Guid id)
        {
            var userId = User.GetUserId();
            var response = await _audioTrackService.GetTrackByIdAsync(id, userId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Lấy link stream tạm thời (presigned URL có thời hạn 1 tiếng)
        /// </summary>
        [HttpGet("{id:guid}/stream-url")]
        public async Task<IActionResult> GetStreamUrl(Guid id)
        {
            var userId = User.GetUserId();
            var response = await _audioTrackService.GetStreamUrlAsync(id, userId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Lấy danh sách danh mục hoạt động (sắp xếp theo SortOrder)
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var response = await _audioTrackService.GetCategoriesAsync();
            return HandleResponse(response);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách danh mục kể cả danh mục bị vô hiệu hóa (Tất cả người dùng)
        /// </summary>
        [HttpGet("categories/all")]
        public async Task<IActionResult> GetCategoriesForAdmin()
        {
            var adminUserId = User.GetUserId();
            var response = await _audioTrackService.GetAllCategoriesForAdminAsync(adminUserId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một danh mục theo ID
        /// </summary>
        [HttpGet("categories/{id:guid}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var response = await _audioTrackService.GetCategoryByIdAsync(id);
            return HandleResponse(response);
        }

        // ==========================================
        // --- TRACK CRUD ENDPOINTS (Admin Only) ---
        // ==========================================

        /// <summary>
        /// Tạo bài hát mới (Chỉ Admin)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTrack([FromBody] CreateAudioTrackDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu đầu vào không hợp lệ", ErrorCode.VALIDATION_ERROR));
            }

            var adminUserId = User.GetUserId();
            var response = await _audioTrackService.CreateTrackAsync(dto, adminUserId);
            if (response.Success)
            {
                return CreatedAtAction(nameof(GetTrackById), new { id = response.Data?.Id }, response);
            }
            return HandleResponse(response);
        }

        /// <summary>
        /// Cập nhật thông tin bài hát (Chỉ Admin)
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTrack(Guid id, [FromBody] UpdateAudioTrackDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu đầu vào không hợp lệ", ErrorCode.VALIDATION_ERROR));
            }

            var adminUserId = User.GetUserId();
            var response = await _audioTrackService.UpdateTrackAsync(id, dto, adminUserId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Xóa bài hát (Xóa mềm - Chỉ Admin)
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTrack(Guid id)
        {
            var adminUserId = User.GetUserId();
            var response = await _audioTrackService.DeleteTrackAsync(id, adminUserId);
            return HandleResponse(response);
        }

        // ==========================================
        // --- CATEGORY CRUD ENDPOINTS (Admin Only) --
        // ==========================================

        /// <summary>
        /// Tạo danh mục bài hát mới (Chỉ Admin)
        /// </summary>
        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateAudioCategoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu đầu vào không hợp lệ", ErrorCode.VALIDATION_ERROR));
            }

            var adminUserId = User.GetUserId();
            var response = await _audioTrackService.CreateCategoryAsync(dto, adminUserId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Cập nhật thông tin danh mục (Chỉ Admin)
        /// </summary>
        [HttpPut("categories/{id:guid}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateAudioCategoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu đầu vào không hợp lệ", ErrorCode.VALIDATION_ERROR));
            }

            var adminUserId = User.GetUserId();
            var response = await _audioTrackService.UpdateCategoryAsync(id, dto, adminUserId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Xóa danh mục bài hát (Chỉ Admin - chỉ cho phép khi không có bài hát nào thuộc danh mục)
        /// </summary>
        [HttpDelete("categories/{id:guid}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var adminUserId = User.GetUserId();
            var response = await _audioTrackService.DeleteCategoryAsync(id, adminUserId);
            return HandleResponse(response);
        }

        // ==========================================
        // --- PLAY HISTORY & STATS ENDPOINTS -------
        // ==========================================

        /// <summary>
        /// Ghi nhận lịch sử nghe nhạc (Tăng playCount atomic + lưu lịch sử)
        /// </summary>
        [HttpPost("play")]
        public async Task<IActionResult> RecordPlay([FromBody] RecordPlayDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Dữ liệu đầu vào không hợp lệ", ErrorCode.VALIDATION_ERROR));
            }

            var userId = User.GetUserId();
            var response = await _audioTrackService.RecordPlayAsync(dto, userId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Lấy lịch sử nghe nhạc của người dùng (phân trang, sắp xếp mới nhất trước)
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetPlayHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();
            var response = await _audioTrackService.GetPlayHistoryAsync(userId, page, pageSize);
            return HandleResponse(response);
        }

        /// <summary>
        /// Lấy số liệu thống kê nghe nhạc của người dùng (số bài, tổng thời lượng, category nghe nhiều nhất)
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetListeningStats()
        {
            var userId = User.GetUserId();
            var response = await _audioTrackService.GetListeningStatsAsync(userId);
            return HandleResponse(response);
        }

        // ==========================================
        // --- FAVORITES ENDPOINTS ------------------
        // ==========================================

        /// <summary>
        /// Thêm bài hát vào danh sách yêu thích
        /// </summary>
        [HttpPost("{id:guid}/favorite")]
        public async Task<IActionResult> AddFavorite(Guid id)
        {
            var userId = User.GetUserId();
            var response = await _audioTrackService.AddFavoriteAsync(id, userId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Xóa bài hát khỏi danh sách yêu thích
        /// </summary>
        [HttpDelete("{id:guid}/favorite")]
        public async Task<IActionResult> RemoveFavorite(Guid id)
        {
            var userId = User.GetUserId();
            var response = await _audioTrackService.RemoveFavoriteAsync(id, userId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Lấy danh sách bài hát yêu thích (phân trang, sắp xếp mới yêu thích trước)
        /// </summary>
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();
            var response = await _audioTrackService.GetFavoritesAsync(userId, page, pageSize);
            return HandleResponse(response);
        }
    }
}
