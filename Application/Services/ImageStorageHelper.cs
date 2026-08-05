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
            string base64Data = base64String.Trim();

            if (base64Data.Contains(","))
            {
                var parts = base64Data.Split(',', 2);
                var header = parts[0].ToLower();
                base64Data = parts[1];

                if (header.Contains("jpeg") || header.Contains("jpg")) extension = ".jpg";
                else if (header.Contains("gif")) extension = ".gif";
                else if (header.Contains("webp")) extension = ".webp";
                else extension = ".png";
            }

            // Clean up any newlines or whitespace in base64 string
            base64Data = Regex.Replace(base64Data, @"\s+", "");

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
                if (cleanHost.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !cleanHost.Contains("localhost"))
                {
                    cleanHost = "https://" + cleanHost.Substring(7);
                }
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
