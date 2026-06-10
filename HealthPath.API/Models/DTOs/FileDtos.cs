using System;
using System.Collections.Generic;

namespace HealthPath.API.Models.DTOs;

public class FileUploadResultDto
{
    public string Url { get; set; } = null!;
    public string FileKey { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
}

public class StoredAudioFileDto
{
    public string FileKey { get; set; } = null!;
    public string Url { get; set; } = null!;
    public long SizeBytes { get; set; }
    public DateTime? UploadedAt { get; set; }
    public bool IsRegistered { get; set; }
    public Guid? TrackId { get; set; }
    public string? TrackTitle { get; set; }
}

public class AudioTrackRegistrationFieldDto
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool Required { get; set; }
    public string Description { get; set; } = null!;
    public object? Example { get; set; }
}

public class AudioTrackRegistrationInfoDto
{
    public string CreateTrackEndpoint { get; set; } = "POST /api/AudioTrack";
    public List<string> Steps { get; set; } = new();
    public List<AudioCategoryDto> Categories { get; set; } = new();
    public List<AudioTrackRegistrationFieldDto> Fields { get; set; } = new();
    public CreateAudioTrackDto SuggestedBody { get; set; } = new();
}
