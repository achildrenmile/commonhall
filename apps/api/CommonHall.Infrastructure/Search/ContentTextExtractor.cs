using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CommonHall.Infrastructure.Search;

public static partial class ContentTextExtractor
{
    /// <summary>
    /// Extracts plain text from JSONB widget content
    /// </summary>
    public static string ExtractText(string? jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var sb = new StringBuilder();
            ExtractTextFromElement(doc.RootElement, sb);
            return sb.ToString().Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void ExtractTextFromElement(JsonElement element, StringBuilder sb)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractTextFromElement(item, sb);
                }
                break;

            case JsonValueKind.Object:
                // Check for common text content properties
                if (element.TryGetProperty("content", out var content))
                {
                    ExtractTextFromElement(content, sb);
                }

                if (element.TryGetProperty("text", out var text))
                {
                    if (text.ValueKind == JsonValueKind.String)
                    {
                        var textValue = text.GetString();
                        if (!string.IsNullOrWhiteSpace(textValue))
                        {
                            sb.Append(StripHtml(textValue));
                            sb.Append(' ');
                        }
                    }
                    else
                    {
                        ExtractTextFromElement(text, sb);
                    }
                }

                if (element.TryGetProperty("html", out var html) && html.ValueKind == JsonValueKind.String)
                {
                    var htmlValue = html.GetString();
                    if (!string.IsNullOrWhiteSpace(htmlValue))
                    {
                        sb.Append(StripHtml(htmlValue));
                        sb.Append(' ');
                    }
                }

                if (element.TryGetProperty("body", out var body))
                {
                    if (body.ValueKind == JsonValueKind.String)
                    {
                        var bodyValue = body.GetString();
                        if (!string.IsNullOrWhiteSpace(bodyValue))
                        {
                            sb.Append(StripHtml(bodyValue));
                            sb.Append(' ');
                        }
                    }
                    else
                    {
                        ExtractTextFromElement(body, sb);
                    }
                }

                // Extract alt text from images
                if (element.TryGetProperty("alt", out var alt) && alt.ValueKind == JsonValueKind.String)
                {
                    var altValue = alt.GetString();
                    if (!string.IsNullOrWhiteSpace(altValue))
                    {
                        sb.Append(altValue);
                        sb.Append(' ');
                    }
                }

                // Extract caption
                if (element.TryGetProperty("caption", out var caption) && caption.ValueKind == JsonValueKind.String)
                {
                    var captionValue = caption.GetString();
                    if (!string.IsNullOrWhiteSpace(captionValue))
                    {
                        sb.Append(captionValue);
                        sb.Append(' ');
                    }
                }

                // Recurse into children/items
                if (element.TryGetProperty("children", out var children))
                {
                    ExtractTextFromElement(children, sb);
                }

                if (element.TryGetProperty("items", out var items))
                {
                    ExtractTextFromElement(items, sb);
                }

                if (element.TryGetProperty("rows", out var rows))
                {
                    ExtractTextFromElement(rows, sb);
                }

                if (element.TryGetProperty("cells", out var cells))
                {
                    ExtractTextFromElement(cells, sb);
                }
                break;

            case JsonValueKind.String:
                var strValue = element.GetString();
                if (!string.IsNullOrWhiteSpace(strValue))
                {
                    sb.Append(StripHtml(strValue));
                    sb.Append(' ');
                }
                break;
        }
    }

    private static string StripHtml(string html)
    {
        // Remove HTML tags
        var text = HtmlTagRegex().Replace(html, " ");
        // Decode common HTML entities
        text = text.Replace("&nbsp;", " ")
                   .Replace("&amp;", "&")
                   .Replace("&lt;", "<")
                   .Replace("&gt;", ">")
                   .Replace("&quot;", "\"")
                   .Replace("&#39;", "'");
        // Normalize whitespace
        text = WhitespaceRegex().Replace(text, " ");
        return text.Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
