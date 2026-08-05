using System.Text.RegularExpressions;

namespace movaa_project_back.Application.Services;

public static class ImageStorageHelper
{
    /// <summary>
    /// Saves a Base64 encoded image string to the server filesystem under wwwroot/uploads/{category}/.
    /// Category folder default is "general" (or "salons", "specialists", "users", "offers").
    /// Returns the relative path or full URL.
    /// </summary>
    public static string? SaveBase64Image(string? base64String, string contentRootPath, string? hostUrl = null, string category = "general")
    {
        if (string.IsNullOrWhiteSpace(base64String)) return null;

        // If it's already an HTTP/HTTPS URL, return as is
        if (base64String.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            base64String.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return base64String;
        }

        // If it's already a relative path (/uploads/ or /logos/), convert to full host URL if available
        if (base64String.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ||
            base64String.StartsWith("/logos/", StringComparison.OrdinalIgnoreCase))
        {
            return FormatFullUrl(base64String, hostUrl);
        }

        try
        {
            string extension = ".jpg";
            string base64Data = base64String.Trim();

            if (base64Data.Contains(","))
            {
                var parts = base64Data.Split(',', 2);
                var header = parts[0].ToLower();
                base64Data = parts[1];

                if (header.Contains("png")) extension = ".png";
                else if (header.Contains("webp")) extension = ".webp";
                else if (header.Contains("gif")) extension = ".gif";
                else extension = ".jpg";
            }

            // Clean up whitespace/newlines
            base64Data = Regex.Replace(base64Data, @"\s+", "");
            byte[] imageBytes = Convert.FromBase64String(base64Data);

            // Create categorized folder: wwwroot/uploads/{category}
            var uploadsFolderPath = Path.Combine(contentRootPath, "wwwroot", "uploads", category);
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            var fileName = $"{category}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolderPath, fileName);

            File.WriteAllBytes(filePath, imageBytes);

            var relativePath = $"/uploads/{category}/{fileName}";
            return FormatFullUrl(relativePath, hostUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ImageStorageHelper] Error saving base64 image in category '{category}': {ex.Message}");
            return null;
        }
    }

    private static string FormatFullUrl(string relativePath, string? hostUrl)
    {
        if (string.IsNullOrWhiteSpace(hostUrl)) return relativePath;

        var cleanHost = hostUrl.TrimEnd('/');
        if (cleanHost.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !cleanHost.Contains("localhost"))
        {
            cleanHost = "https://" + cleanHost.Substring(7);
        }
        return $"{cleanHost}{relativePath}";
    }
}
