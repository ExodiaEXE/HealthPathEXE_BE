using System;
using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models.DTOs;

// --- Response DTOs ---

public class AudioTrackDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Artist { get; set; }
    public string? Studio { get; set; }
    public string Category { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public int DurationSeconds { get; set; }
    public string? CoverUrl { get; set; }
    public bool IsPremium { get; set; }
    public long PlayCount { get; set; }
    public bool IsFavorited { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AudioTrackDetailDto : AudioTrackDto
{
    public Guid? UploadedBy { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AudioCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class AudioHistoryDto
{
    public Guid Id { get; set; }
    public Guid TrackId { get; set; }
    public string TrackTitle { get; set; } = null!;
    public string? TrackCoverUrl { get; set; }
    public string? TrackArtist { get; set; }
    public string TrackCategory { get; set; } = null!;
    public int PlayedSeconds { get; set; }
    public DateTime PlayedAt { get; set; }
}

public class AudioStatsDto
{
    public int TotalTracksPlayed { get; set; }
    public long TotalSecondsListened { get; set; }
    public string? MostPlayedCategory { get; set; }
}

public class AudioStreamUrlDto
{
    public string StreamUrl { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

// --- Request DTOs ---

public class CreateAudioTrackDto
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; } = null!;

    [MaxLength(150, ErrorMessage = "Tên nghệ sĩ không được vượt quá 150 ký tự")]
    public string? Artist { get; set; }

    [MaxLength(150, ErrorMessage = "Studio không được vượt quá 150 ký tự")]
    public string? Studio { get; set; }

    [Required(ErrorMessage = "CategoryId không được để trống")]
    public Guid CategoryId { get; set; }

    [Range(1, 36000, ErrorMessage = "Thời lượng phải từ 1 giây đến 10 tiếng")]
    public int DurationSeconds { get; set; }

    [Required(ErrorMessage = "FileUrl (Key) không được để trống")]
    public string FileUrl { get; set; } = null!; // Key trên R2, ví dụ: audio/tracks/uuid.mp3

    public string? CoverUrl { get; set; } // Public URL từ R2

    public bool IsPremium { get; set; } = false;
}

public class UpdateAudioTrackDto
{
    [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string? Title { get; set; }

    [MaxLength(150, ErrorMessage = "Tên nghệ sĩ không được vượt quá 150 ký tự")]
    public string? Artist { get; set; }

    [MaxLength(150, ErrorMessage = "Studio không được vượt quá 150 ký tự")]
    public string? Studio { get; set; }

    public Guid? CategoryId { get; set; }

    [Range(1, 36000, ErrorMessage = "Thời lượng phải từ 1 giây đến 10 tiếng")]
    public int? DurationSeconds { get; set; }

    public string? CoverUrl { get; set; }

    public bool? IsPremium { get; set; }

    public bool? IsActive { get; set; }
}

public class CreateAudioCategoryDto
{
    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    [MaxLength(50, ErrorMessage = "Tên danh mục không được vượt quá 50 ký tự")]
    public string Name { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public int SortOrder { get; set; } = 0;
}

public class UpdateAudioCategoryDto
{
    [MaxLength(50, ErrorMessage = "Tên danh mục không được vượt quá 50 ký tự")]
    public string? Name { get; set; }

    [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public int? SortOrder { get; set; }

    public bool? IsActive { get; set; }
}

public class RecordPlayDto
{
    [Required(ErrorMessage = "TrackId không được để trống")]
    public Guid TrackId { get; set; }

    [Range(1, 36000, ErrorMessage = "Số giây nghe phải từ 1 giây đến 10 tiếng")]
    public int PlayedSeconds { get; set; }
}
