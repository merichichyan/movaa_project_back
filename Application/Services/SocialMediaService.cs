using movaa_project_back.Domain.Enums;

namespace movaa_project_back.Application.Services;

public static class SocialMediaService
{
    public static string NormalizeUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            throw new ArgumentException("URL cannot be empty.");

        var trimmed = rawUrl.Trim();

        // Prevent dangerous non-web schemes
        var lower = trimmed.ToLowerInvariant();
        if (lower.StartsWith("javascript:") || lower.StartsWith("data:") || lower.StartsWith("file:") || lower.StartsWith("vbscript:"))
        {
            throw new ArgumentException("Invalid or dangerous URL scheme.");
        }

        // Add https:// scheme if missing
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "https://" + trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL format.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Only HTTP and HTTPS URLs are supported.");
        }

        return uri.AbsoluteUri;
    }

    public static SocialPlatform DetectPlatform(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return SocialPlatform.Website;

        try
        {
            var normalized = NormalizeUrl(url);
            var uri = new Uri(normalized);
            var host = uri.Host.ToLowerInvariant();

            if (host.Contains("instagram.com") || host.Contains("instagr.am"))
                return SocialPlatform.Instagram;

            if (host.Contains("facebook.com") || host.Contains("fb.com") || host.Contains("fb.me"))
                return SocialPlatform.Facebook;

            if (host.Contains("tiktok.com"))
                return SocialPlatform.TikTok;

            if (host.Equals("t.me") || host.EndsWith(".t.me") || host.Contains("telegram.me") || host.Contains("telegram.org"))
                return SocialPlatform.Telegram;

            if (host.Equals("wa.me") || host.EndsWith(".wa.me") || host.Contains("whatsapp.com"))
                return SocialPlatform.WhatsApp;

            if (host.Contains("viber.com") || host.Contains("viber.me"))
                return SocialPlatform.Viber;

            if (host.Contains("youtube.com") || host.Contains("youtu.be"))
                return SocialPlatform.YouTube;

            if (host.Contains("linkedin.com"))
                return SocialPlatform.LinkedIn;

            if (host.Contains("twitter.com") || host.Equals("x.com") || host.EndsWith(".x.com"))
                return SocialPlatform.X;

            return SocialPlatform.Website;
        }
        catch
        {
            return SocialPlatform.Website;
        }
    }
}
