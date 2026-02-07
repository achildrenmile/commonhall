using System.Text;
using System.Text.RegularExpressions;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommonHall.Infrastructure.Services;

public sealed partial class TagService : ITagService
{
    private readonly IApplicationDbContext _context;

    public TagService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Tag>> SyncTagsAsync(Guid articleId, List<string> tagNames, CancellationToken cancellationToken = default)
    {
        // Get or create tags
        var tags = await GetOrCreateTagsAsync(tagNames, cancellationToken);
        var tagIds = tags.Select(t => t.Id).ToHashSet();

        // Get existing article-tag relationships
        var existingArticleTags = await _context.ArticleTags
            .Where(at => at.NewsArticleId == articleId)
            .ToListAsync(cancellationToken);

        var existingTagIds = existingArticleTags.Select(at => at.TagId).ToHashSet();

        // Remove tags that are no longer associated
        var tagsToRemove = existingArticleTags.Where(at => !tagIds.Contains(at.TagId)).ToList();
        foreach (var articleTag in tagsToRemove)
        {
            _context.ArticleTags.Remove(articleTag);
        }

        // Add new tag associations
        var tagIdsToAdd = tagIds.Except(existingTagIds);
        foreach (var tagId in tagIdsToAdd)
        {
            _context.ArticleTags.Add(new ArticleTag
            {
                NewsArticleId = articleId,
                TagId = tagId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return tags;
    }

    public async Task<List<Tag>> GetOrCreateTagsAsync(List<string> tagNames, CancellationToken cancellationToken = default)
    {
        if (tagNames.Count == 0)
            return [];

        // Normalize tag names and generate slugs
        var normalizedTags = tagNames
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(n => new { Name = n, Slug = GenerateTagSlug(n) })
            .ToList();

        if (normalizedTags.Count == 0)
            return [];

        var slugs = normalizedTags.Select(t => t.Slug).ToList();

        // Find existing tags
        var existingTags = await _context.Tags
            .Where(t => slugs.Contains(t.Slug))
            .ToListAsync(cancellationToken);

        var existingSlugs = existingTags.Select(t => t.Slug).ToHashSet();

        // Create missing tags
        var tagsToCreate = normalizedTags
            .Where(t => !existingSlugs.Contains(t.Slug))
            .Select(t => new Tag
            {
                Name = t.Name,
                Slug = t.Slug
            })
            .ToList();

        if (tagsToCreate.Count > 0)
        {
            _context.Tags.AddRange(tagsToCreate);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return [.. existingTags, .. tagsToCreate];
    }

    private static string GenerateTagSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Convert to lowercase
        var slug = name.ToLowerInvariant();

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

        // Limit length
        if (slug.Length > 100)
        {
            slug = slug[..100].TrimEnd('-');
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
