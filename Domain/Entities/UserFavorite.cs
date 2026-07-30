using System;

namespace movaa_project_back.Domain.Entities
{
    public class UserFavorite
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "salon" or "specialist"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public UserFavorite()
        {
            Id = Guid.NewGuid();
        }

        public UserFavorite(Guid userId, string targetId, string type) : this()
        {
            UserId = userId;
            TargetId = targetId.Trim();
            Type = type.Trim().ToLowerInvariant();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
