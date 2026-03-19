using WebApit4s.Identity;

namespace WebApit4s.Models
{
    public class UserRefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // FK to AspNetUsers
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        // Store a HASH, not the raw token
        public string TokenHash { get; set; } = null!;

        // For rotation / families (optional but recommended)
        public string? ParentTokenHash { get; set; }
        public string? FamilyId { get; set; }   // same across a rotation chain

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UsedUtc { get; set; }   // first use time
        public DateTime ExpiresUtc { get; set; }
        public DateTime? RevokedUtc { get; set; }

        // Context for forensics & selective revocation
        public string? DeviceId { get; set; }      // app-generated GUID per install
        public string? DeviceName { get; set; }    // e.g., “iPhone 13”
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }

        // Convenience flags
        public bool IsRevoked => RevokedUtc.HasValue;
        public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
