using System.Text;
using System.Text.RegularExpressions;
using CommonHall.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Infrastructure.Services;

public sealed partial class SlugService : ISlugService
{
    private readonly IApplicationDbContext _context;
    private const int MaxSlugLength = 200;

    public SlugService(IApplicationDbContext context)
    {
        _context = context;
    }

    public string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        // Convert to lowercase
        var slug = title.ToLowerInvariant();

        // Remove accents/diacritics
        slug = RemoveDiacritics(slug);

        // Replace spaces with hyphens
        slug = slug.Replace(' ', '-');

        // Remove special characters, keep only alphanumeric and hyphens
        slug = AlphanumericAndHyphenRegex().Replace(slug, string.Empty);

        // Replace multiple consecutive hyphens with single hyphen
        slug = MultipleHyphensRegex().Replace(slug, "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        // Truncate to max length, but don't cut in the middle of a word
        if (slug.Length > MaxSlugLength)
        {
            slug = slug[..MaxSlugLength];
            var lastHyphen = slug.LastIndexOf('-');
            if (lastHyphen > MaxSlugLength - 20)
            {
                slug = slug[..lastHyphen];
            }
        }

        return slug;
    }

    public async Task<string> GenerateUniqueSpaceSlugAsync(string title, CancellationToken cancellationToken = default)
    {
        var baseSlug = GenerateSlug(title);
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "space";

        var slug = baseSlug;
        var counter = 1;

        while (await _context.Spaces.AnyAsync(s => s.Slug == slug, cancellationToken))
        {
            counter++;
            slug = $"{baseSlug}-{counter}";

            // Ensure we don't exceed max length
            if (slug.Length > MaxSlugLength)
            {
                var suffix = $"-{counter}";
                baseSlug = baseSlug[..(MaxSlugLength - suffix.Length)];
                slug = $"{baseSlug}{suffix}";
            }
        }

        return slug;
    }

    public async Task<string> GenerateUniquePageSlugAsync(Guid spaceId, string title, CancellationToken cancellationToken = default)
    {
        var baseSlug = GenerateSlug(title);
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "page";

        var slug = baseSlug;
        var counter = 1;

        while (await _context.Pages.AnyAsync(p => p.SpaceId == spaceId && p.Slug == slug, cancellationToken))
        {
            counter++;
            slug = $"{baseSlug}-{counter}";

            // Ensure we don't exceed max length
            if (slug.Length > MaxSlugLength)
            {
                var suffix = $"-{counter}";
                baseSlug = baseSlug[..(MaxSlugLength - suffix.Length)];
                slug = $"{baseSlug}{suffix}";
            }
        }

        return slug;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex("[^a-z0-9-]")]
    private static partial Regex AlphanumericAndHyphenRegex();

    [GeneratedRegex("-+")]
    private static partial Regex MultipleHyphensRegex();
}
