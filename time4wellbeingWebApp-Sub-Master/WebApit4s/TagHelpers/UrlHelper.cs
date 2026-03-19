namespace WebApit4s.TagHelpers
{
    public static class UrlHelper
    {
        public static string GetAvatarUrl(string? avatarPath, HttpRequest? request = null)
        {
            if (string.IsNullOrWhiteSpace(avatarPath))
                return "/images/default-avatar.png";

            // If it's already a full URL, return as-is
            if (avatarPath.StartsWith("http://") || avatarPath.StartsWith("https://"))
                return avatarPath;

            // ✅ Ensure avatarPath starts with /
            if (!avatarPath.StartsWith("/"))
            {
                avatarPath = "/" + avatarPath;
            }

            // Handle both /characters/ and /images/Characters/ (case-insensitive)
            if (avatarPath.StartsWith("/characters/", StringComparison.OrdinalIgnoreCase))
            {
                avatarPath = avatarPath.Replace("/characters/", "/images/Characters/", StringComparison.OrdinalIgnoreCase);
            }

            // For API calls (MAUI app), return full URL
            if (request != null)
            {
                // ✅ FIX: Ensure proper URL formatting
                var fullUrl = $"{request.Scheme}://{request.Host}{avatarPath}";
                Console.WriteLine($"🖼️ API: Avatar URL = {fullUrl}");
                return fullUrl;
            }

            // For web app, return relative path
            Console.WriteLine($"🖼️ Web: Avatar URL = {avatarPath}");
            return avatarPath;
        }
    }
}