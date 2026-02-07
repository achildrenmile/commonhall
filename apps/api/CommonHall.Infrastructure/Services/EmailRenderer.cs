using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Services;

/// <summary>
/// Renders email newsletters to responsive HTML with inline styles.
/// </summary>
public sealed partial class EmailRenderer : IEmailRenderer
{
    private readonly ILogger<EmailRenderer> _logger;

    public EmailRenderer(ILogger<EmailRenderer> logger)
    {
        _logger = logger;
    }

    public Task<string> RenderToHtmlAsync(
        EmailNewsletter newsletter,
        EmailRecipient recipient,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var html = RenderEmailContent(newsletter, baseUrl, recipient.TrackingToken);
        return Task.FromResult(html);
    }

    public Task<string> RenderPreviewAsync(
        EmailNewsletter newsletter,
        CancellationToken cancellationToken = default)
    {
        var html = RenderEmailContent(newsletter, null, null);
        return Task.FromResult(html);
    }

    private string RenderEmailContent(EmailNewsletter newsletter, string? baseUrl, string? trackingToken)
    {
        var sb = new StringBuilder();

        // Email wrapper with responsive styles
        sb.AppendLine(GetEmailHeader(newsletter.PreviewText));

        // Parse and render content blocks
        try
        {
            using var doc = JsonDocument.Parse(newsletter.Content);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in doc.RootElement.EnumerateArray())
                {
                    var blockHtml = RenderBlock(block, baseUrl, trackingToken);
                    sb.AppendLine(blockHtml);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse newsletter content");
            sb.AppendLine("<tr><td>Content could not be rendered.</td></tr>");
        }

        // Add tracking pixel if we have a tracking token
        if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(trackingToken))
        {
            sb.AppendLine(GetTrackingPixel(baseUrl, trackingToken));
        }

        sb.AppendLine(GetEmailFooter());

        var html = sb.ToString();

        // Rewrite links for click tracking
        if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(trackingToken))
        {
            html = RewriteLinksForTracking(html, baseUrl, trackingToken);
        }

        return html;
    }

    private string RenderBlock(JsonElement block, string? baseUrl, string? trackingToken)
    {
        var type = block.TryGetProperty("type", out var typeProp)
            ? typeProp.GetString() ?? "text"
            : "text";
        var data = block.TryGetProperty("data", out var dataProp) ? dataProp : default;

        return type switch
        {
            "header" => RenderHeaderBlock(data),
            "heading" => RenderHeadingBlock(data),
            "text" => RenderTextBlock(data),
            "image" => RenderImageBlock(data),
            "button" => RenderButtonBlock(data),
            "divider" => RenderDividerBlock(),
            "columns" => RenderColumnsBlock(data, baseUrl, trackingToken),
            "spacer" => RenderSpacerBlock(data),
            "footer" => RenderFooterBlock(data),
            "news-preview" => RenderNewsPreviewBlock(data),
            _ => $"<!-- Unknown block type: {type} -->"
        };
    }

    private static string RenderHeaderBlock(JsonElement data)
    {
        var logoUrl = data.TryGetProperty("logoUrl", out var logo) ? logo.GetString() : null;
        var title = data.TryGetProperty("title", out var t) ? t.GetString() : null;
        var backgroundColor = data.TryGetProperty("backgroundColor", out var bg) ? bg.GetString() : "#1e293b";
        var textColor = data.TryGetProperty("textColor", out var tc) ? tc.GetString() : "#ffffff";

        var sb = new StringBuilder();
        sb.AppendLine($@"<tr>
            <td style=""background-color: {backgroundColor}; padding: 24px; text-align: center;"">");

        if (!string.IsNullOrEmpty(logoUrl))
        {
            sb.AppendLine($@"<img src=""{HttpUtility.HtmlEncode(logoUrl)}"" alt=""Logo"" style=""max-height: 50px; margin-bottom: 16px;"" />");
        }

        if (!string.IsNullOrEmpty(title))
        {
            sb.AppendLine($@"<h1 style=""margin: 0; color: {textColor}; font-size: 24px; font-weight: bold;"">{HttpUtility.HtmlEncode(title)}</h1>");
        }

        sb.AppendLine("</td></tr>");
        return sb.ToString();
    }

    private static string RenderHeadingBlock(JsonElement data)
    {
        var text = data.TryGetProperty("text", out var t) ? t.GetString() : "";
        var level = data.TryGetProperty("level", out var l) ? l.GetInt32() : 2;
        var alignment = data.TryGetProperty("alignment", out var a) ? a.GetString() : "left";

        var fontSize = level switch
        {
            1 => "28px",
            2 => "24px",
            3 => "20px",
            _ => "18px"
        };

        return $@"<tr>
            <td style=""padding: 16px 24px;"">
                <h{level} style=""margin: 0; font-size: {fontSize}; font-weight: bold; color: #1e293b; text-align: {alignment};"">{HttpUtility.HtmlEncode(text)}</h{level}>
            </td>
        </tr>";
    }

    private static string RenderTextBlock(JsonElement data)
    {
        var html = data.TryGetProperty("html", out var h) ? h.GetString() : "";

        // Sanitize HTML - in production, use a proper HTML sanitizer
        // For now, we'll allow basic formatting tags

        return $@"<tr>
            <td style=""padding: 8px 24px; font-size: 16px; line-height: 1.6; color: #374151;"">
                {html}
            </td>
        </tr>";
    }

    private static string RenderImageBlock(JsonElement data)
    {
        var src = data.TryGetProperty("src", out var s) ? s.GetString() : "";
        var alt = data.TryGetProperty("alt", out var a) ? a.GetString() : "";
        var width = data.TryGetProperty("width", out var w) ? w.GetString() : "100%";
        var alignment = data.TryGetProperty("alignment", out var align) ? align.GetString() : "center";
        var linkUrl = data.TryGetProperty("linkUrl", out var link) ? link.GetString() : null;

        var imgTag = $@"<img src=""{HttpUtility.HtmlEncode(src)}"" alt=""{HttpUtility.HtmlEncode(alt)}"" style=""max-width: {width}; height: auto; display: block;"" />";

        if (!string.IsNullOrEmpty(linkUrl))
        {
            imgTag = $@"<a href=""{HttpUtility.HtmlEncode(linkUrl)}"" target=""_blank"">{imgTag}</a>";
        }

        return $@"<tr>
            <td style=""padding: 16px 24px; text-align: {alignment};"">
                {imgTag}
            </td>
        </tr>";
    }

    private static string RenderButtonBlock(JsonElement data)
    {
        var text = data.TryGetProperty("text", out var t) ? t.GetString() : "Click Here";
        var url = data.TryGetProperty("url", out var u) ? u.GetString() : "#";
        var alignment = data.TryGetProperty("alignment", out var a) ? a.GetString() : "center";
        var backgroundColor = data.TryGetProperty("backgroundColor", out var bg) ? bg.GetString() : "#3b82f6";
        var textColor = data.TryGetProperty("textColor", out var tc) ? tc.GetString() : "#ffffff";

        return $@"<tr>
            <td style=""padding: 16px 24px; text-align: {alignment};"">
                <a href=""{HttpUtility.HtmlEncode(url)}"" target=""_blank"" style=""display: inline-block; padding: 12px 32px; background-color: {backgroundColor}; color: {textColor}; text-decoration: none; font-weight: 600; border-radius: 6px; font-size: 16px;"">{HttpUtility.HtmlEncode(text)}</a>
            </td>
        </tr>";
    }

    private static string RenderDividerBlock()
    {
        return @"<tr>
            <td style=""padding: 16px 24px;"">
                <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 0;"" />
            </td>
        </tr>";
    }

    private string RenderColumnsBlock(JsonElement data, string? baseUrl, string? trackingToken)
    {
        var columns = data.TryGetProperty("columns", out var cols) ? cols : default;
        if (columns.ValueKind != JsonValueKind.Array)
            return "";

        var sb = new StringBuilder();
        sb.AppendLine(@"<tr><td style=""padding: 0 24px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                <tr>");

        var colCount = 0;
        foreach (var _ in columns.EnumerateArray()) colCount++;
        var colWidth = 100 / (colCount > 0 ? colCount : 1);

        foreach (var column in columns.EnumerateArray())
        {
            sb.AppendLine($@"<td style=""width: {colWidth}%; vertical-align: top; padding: 8px;"">");

            if (column.TryGetProperty("blocks", out var blocks) && blocks.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine(@"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"">");
                foreach (var block in blocks.EnumerateArray())
                {
                    sb.AppendLine(RenderBlock(block, baseUrl, trackingToken));
                }
                sb.AppendLine("</table>");
            }

            sb.AppendLine("</td>");
        }

        sb.AppendLine("</tr></table></td></tr>");
        return sb.ToString();
    }

    private static string RenderSpacerBlock(JsonElement data)
    {
        var height = data.TryGetProperty("height", out var h) ? h.GetInt32() : 24;
        return $@"<tr><td style=""height: {height}px; line-height: {height}px; font-size: 1px;"">&nbsp;</td></tr>";
    }

    private static string RenderFooterBlock(JsonElement data)
    {
        var text = data.TryGetProperty("text", out var t) ? t.GetString() : "";
        var showUnsubscribe = data.TryGetProperty("showUnsubscribe", out var unsub) && unsub.GetBoolean();
        var backgroundColor = data.TryGetProperty("backgroundColor", out var bg) ? bg.GetString() : "#f8fafc";

        var sb = new StringBuilder();
        sb.AppendLine($@"<tr>
            <td style=""background-color: {backgroundColor}; padding: 24px; text-align: center; font-size: 12px; color: #6b7280;"">");

        if (!string.IsNullOrEmpty(text))
        {
            sb.AppendLine($@"<p style=""margin: 0 0 8px 0;"">{HttpUtility.HtmlEncode(text)}</p>");
        }

        if (showUnsubscribe)
        {
            sb.AppendLine(@"<p style=""margin: 0;""><a href=""{{unsubscribe_url}}"" style=""color: #6b7280; text-decoration: underline;"">Unsubscribe</a></p>");
        }

        sb.AppendLine("</td></tr>");
        return sb.ToString();
    }

    private static string RenderNewsPreviewBlock(JsonElement data)
    {
        var title = data.TryGetProperty("title", out var t) ? t.GetString() : "";
        var teaser = data.TryGetProperty("teaser", out var ts) ? ts.GetString() : "";
        var imageUrl = data.TryGetProperty("imageUrl", out var img) ? img.GetString() : null;
        var linkUrl = data.TryGetProperty("linkUrl", out var link) ? link.GetString() : "#";

        var sb = new StringBuilder();
        sb.AppendLine(@"<tr><td style=""padding: 16px 24px;"">");
        sb.AppendLine(@"<table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #f8fafc; border-radius: 8px; overflow: hidden;"">");

        if (!string.IsNullOrEmpty(imageUrl))
        {
            sb.AppendLine($@"<tr><td><img src=""{HttpUtility.HtmlEncode(imageUrl)}"" alt="""" style=""width: 100%; height: auto; display: block;"" /></td></tr>");
        }

        sb.AppendLine(@"<tr><td style=""padding: 16px;"">");
        sb.AppendLine($@"<h3 style=""margin: 0 0 8px 0; font-size: 18px; color: #1e293b;""><a href=""{HttpUtility.HtmlEncode(linkUrl)}"" style=""color: inherit; text-decoration: none;"">{HttpUtility.HtmlEncode(title)}</a></h3>");
        sb.AppendLine($@"<p style=""margin: 0 0 16px 0; font-size: 14px; color: #6b7280;"">{HttpUtility.HtmlEncode(teaser)}</p>");
        sb.AppendLine($@"<a href=""{HttpUtility.HtmlEncode(linkUrl)}"" style=""color: #3b82f6; text-decoration: none; font-weight: 500;"">Read more &rarr;</a>");
        sb.AppendLine("</td></tr></table></td></tr>");

        return sb.ToString();
    }

    private static string GetEmailHeader(string? previewText)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <title>Newsletter</title>
    <!--[if mso]>
    <noscript>
        <xml>
            <o:OfficeDocumentSettings>
                <o:PixelsPerInch>96</o:PixelsPerInch>
            </o:OfficeDocumentSettings>
        </xml>
    </noscript>
    <![endif]-->
    <style>
        body, table, td {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; }}
        @media only screen and (max-width: 600px) {{
            .email-container {{ width: 100% !important; }}
        }}
    </style>
</head>
<body style=""margin: 0; padding: 0; background-color: #f1f5f9;"">
    {(string.IsNullOrEmpty(previewText) ? "" : $@"<div style=""display: none; max-height: 0; overflow: hidden;"">{HttpUtility.HtmlEncode(previewText)}</div>")}
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #f1f5f9;"">
        <tr>
            <td align=""center"" style=""padding: 24px 16px;"">
                <table role=""presentation"" class=""email-container"" width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #ffffff; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);"">";
    }

    private static string GetEmailFooter()
    {
        return @"                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string GetTrackingPixel(string baseUrl, string trackingToken)
    {
        var trackingUrl = $"{baseUrl}/api/v1/email/track/{trackingToken}/open";
        return $@"<tr><td><img src=""{HttpUtility.HtmlEncode(trackingUrl)}"" width=""1"" height=""1"" alt="""" style=""display: block;"" /></td></tr>";
    }

    private static string RewriteLinksForTracking(string html, string baseUrl, string trackingToken)
    {
        // Match href attributes, excluding tracking URLs and unsubscribe placeholders
        return HrefRegex().Replace(html, match =>
        {
            var url = match.Groups[1].Value;

            // Skip if it's already a tracking URL or a placeholder
            if (url.Contains("/api/v1/email/track/") ||
                url.StartsWith("{") ||
                url == "#" ||
                url.StartsWith("mailto:") ||
                url.StartsWith("tel:"))
            {
                return match.Value;
            }

            var trackingUrl = $"{baseUrl}/api/v1/email/track/{trackingToken}/click?url={HttpUtility.UrlEncode(url)}";
            return $@"href=""{trackingUrl}""";
        });
    }

    [GeneratedRegex(@"href=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex HrefRegex();
}
