using System.Text.RegularExpressions;

namespace movaa_project_back.Application.Services;

public static class ImageStorageHelper
{
    public static string? SaveBase64Image(string? base64String, string webRootPath, string? hostUrl = null)
    {
        if (string.IsNullOrWhiteSpace(base64String)) return null;

        // If it's already an HTTP URL or existing static path, keep as is
        if (base64String.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            base64String.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            base64String.StartsWith("/logos/", StringComparison.OrdinalIgnoreCase))
        {
            return base64String;
        }

        try
        {
            string extension = ".png";
            string base64Data = base64String;

            if (base64String.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(base64String, @"data:image/(?<type>.*?);base64,(?<data>.*)");
                if (match.Success)
                {
                    var type = match.Groups["type"].Value.ToLower();
                    extension = type switch
                    {
                        "jpeg" or "jpg" => ".jpg",
                        "gif" => ".gif",
                        "webp" => ".webp",
                        _ => ".png"
                    };
                    base64Data = match.Groups["data"].Value;
                }
            }

            byte[] imageBytes = Convert.FromBase64String(base64Data);

            var folderPath = Path.Combine(webRootPath, "wwwroot", "logos");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileName = $"logo_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(folderPath, fileName);

            File.WriteAllBytes(filePath, imageBytes);

            if (!string.IsNullOrWhiteSpace(hostUrl))
            {
                var cleanHost = hostUrl.TrimEnd('/');
                return $"{cleanHost}/logos/{fileName}";
            }

            return $"/logos/{fileName}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ImageStorageHelper] Error decoding/saving logo image: {ex.Message}");
            return null;
        }
    }
}
