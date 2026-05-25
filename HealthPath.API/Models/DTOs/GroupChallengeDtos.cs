using System;
using System.ComponentModel.DataAnnotations;

namespace HealthPath.API.Models.DTOs
{
    public class CreateGroupChallengeDto
    {
        [Required]
        public Guid GroupId { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public DateTime StartsAt { get; set; }

        [Required]
        public DateTime EndsAt { get; set; }
    }
}