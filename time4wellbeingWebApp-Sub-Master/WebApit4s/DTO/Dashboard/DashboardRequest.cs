namespace WebApit4s.DTO.Dashboard
{
    /// <summary>
    /// Client asks for a dashboard snapshot.
    /// Server will resolve the user from Identity; ActiveChildId is optional.
    /// </summary>
    public class DashboardRequest
    {
        /// <summary>Optional child context. If null, server uses the user's active child (if any).</summary>
        public int? ActiveChildId { get; set; }

        /// <summary>How many recent notifications to return (default 5).</summary>
        public int NotificationsTake { get; set; } = 5;

        /// <summary>How many recent activities/logs to return (default 5).</summary>
        public int ActivitiesTake { get; set; } = 5;

        /// <summary>Optional: client's app version (for server-side compatibility logic).</summary>
        public string? ClientVersion { get; set; }
    }
}
