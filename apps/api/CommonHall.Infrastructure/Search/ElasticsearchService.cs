using System.Text;
using System.Text.Json;
using CommonHall.Application.Interfaces;
using CommonHall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommonHall.Infrastructure.Search;

public sealed class ElasticsearchService : ISearchService
{
    private readonly HttpClient _httpClient;
    private readonly CommonHallDbContext _dbContext;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public ElasticsearchService(
        HttpClient httpClient,
        CommonHallDbContext dbContext,
        IConfiguration configuration,
        ILogger<ElasticsearchService> logger)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _logger = logger;
        _baseUrl = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task EnsureIndexesExistAsync(CancellationToken cancellationToken = default)
    {
        await CreateIndexIfNotExistsAsync(SearchIndexConfiguration.NewsIndex,
            SearchIndexConfiguration.Mappings.NewsMapping, cancellationToken);
        await CreateIndexIfNotExistsAsync(SearchIndexConfiguration.PagesIndex,
            SearchIndexConfiguration.Mappings.PagesMapping, cancellationToken);
        await CreateIndexIfNotExistsAsync(SearchIndexConfiguration.UsersIndex,
            SearchIndexConfiguration.Mappings.UsersMapping, cancellationToken);
        await CreateIndexIfNotExistsAsync(SearchIndexConfiguration.FilesIndex,
            SearchIndexConfiguration.Mappings.FilesMapping, cancellationToken);
    }

    private async Task CreateIndexIfNotExistsAsync(string indexName, object mapping, CancellationToken cancellationToken)
    {
        var response = await _httpClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Head, $"{_baseUrl}/{indexName}"),
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return;

        var settings = new
        {
            settings = new
            {
                number_of_shards = 1,
                number_of_replicas = 0,
                analysis = new
                {
                    analyzer = new
                    {
                        @default = new
                        {
                            type = "standard"
                        }
                    }
                }
            },
            mappings = mapping
        };

        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        response = await _httpClient.PutAsync($"{_baseUrl}/{indexName}", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to create index {Index}: {Error}", indexName, error);
        }
        else
        {
            _logger.LogInformation("Created Elasticsearch index: {Index}", indexName);
        }
    }

    public async Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return new SearchResult();
        }

        var indexes = GetTargetIndexes(query.Type);
        var indexStr = string.Join(",", indexes);

        var searchBody = BuildSearchQuery(query);
        var json = JsonSerializer.Serialize(searchBody, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/{indexStr}/_search",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Search failed: {Error}", error);
            return new SearchResult();
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseSearchResponse(responseJson);
    }

    private static string[] GetTargetIndexes(string? type) => type?.ToLower() switch
    {
        "news" => [SearchIndexConfiguration.NewsIndex],
        "pages" => [SearchIndexConfiguration.PagesIndex],
        "users" => [SearchIndexConfiguration.UsersIndex],
        "files" => [SearchIndexConfiguration.FilesIndex],
        _ => SearchIndexConfiguration.AllIndexes
    };

    private static object BuildSearchQuery(SearchQuery query)
    {
        var must = new List<object>
        {
            new
            {
                multi_match = new
                {
                    query = query.Query,
                    fields = new[] { "title^3", "displayName^3", "originalName^3", "teaserText^2", "email^2", "bodyText", "tags", "department", "location", "jobTitle", "altText" },
                    type = "best_fields",
                    fuzziness = "AUTO"
                }
            }
        };

        var filter = new List<object>();

        if (!string.IsNullOrEmpty(query.SpaceSlug))
        {
            filter.Add(new { term = new { spaceSlug = query.SpaceSlug } });
        }

        return new
        {
            from = query.From,
            size = query.Size,
            query = new
            {
                function_score = new
                {
                    query = new
                    {
                        @bool = new
                        {
                            must,
                            filter
                        }
                    },
                    functions = new object[]
                    {
                        // Recency boost for news
                        new
                        {
                            gauss = new
                            {
                                publishedAt = new
                                {
                                    origin = "now",
                                    scale = "30d",
                                    decay = 0.5
                                }
                            },
                            weight = 1.5
                        },
                        // View count boost
                        new
                        {
                            field_value_factor = new
                            {
                                field = "viewCount",
                                factor = 0.1,
                                modifier = "log1p",
                                missing = 1
                            }
                        }
                    },
                    score_mode = "multiply",
                    boost_mode = "multiply"
                }
            },
            highlight = new
            {
                fields = new
                {
                    title = new { },
                    displayName = new { },
                    originalName = new { },
                    teaserText = new { fragment_size = 150 },
                    bodyText = new { fragment_size = 150, number_of_fragments = 2 }
                },
                pre_tags = new[] { "<mark>" },
                post_tags = new[] { "</mark>" }
            },
            aggs = new
            {
                type_facets = new
                {
                    terms = new { field = "_index", size = 10 }
                },
                space_facets = new
                {
                    terms = new { field = "spaceSlug", size = 20, missing = "none" }
                }
            }
        };
    }

    private SearchResult ParseSearchResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var hits = new List<SearchHit>();
        var total = 0;

        if (root.TryGetProperty("hits", out var hitsObj))
        {
            if (hitsObj.TryGetProperty("total", out var totalObj))
            {
                total = totalObj.TryGetProperty("value", out var val) ? val.GetInt32() : 0;
            }

            if (hitsObj.TryGetProperty("hits", out var hitsArray))
            {
                foreach (var hit in hitsArray.EnumerateArray())
                {
                    hits.Add(ParseHit(hit));
                }
            }
        }

        var typeFacets = new Dictionary<string, int>();
        var spaceFacets = new Dictionary<string, int>();

        if (root.TryGetProperty("aggregations", out var aggs))
        {
            if (aggs.TryGetProperty("type_facets", out var typeFacetsObj) &&
                typeFacetsObj.TryGetProperty("buckets", out var typeBuckets))
            {
                foreach (var bucket in typeBuckets.EnumerateArray())
                {
                    var key = bucket.GetProperty("key").GetString() ?? "";
                    var count = bucket.GetProperty("doc_count").GetInt32();
                    var typeName = key.Replace("commonhall-", "");
                    typeFacets[typeName] = count;
                }
            }

            if (aggs.TryGetProperty("space_facets", out var spaceFacetsObj) &&
                spaceFacetsObj.TryGetProperty("buckets", out var spaceBuckets))
            {
                foreach (var bucket in spaceBuckets.EnumerateArray())
                {
                    var key = bucket.GetProperty("key").GetString() ?? "";
                    var count = bucket.GetProperty("doc_count").GetInt32();
                    if (key != "none")
                    {
                        spaceFacets[key] = count;
                    }
                }
            }
        }

        return new SearchResult
        {
            Hits = hits,
            Total = total,
            TypeFacets = typeFacets,
            SpaceFacets = spaceFacets
        };
    }

    private static SearchHit ParseHit(JsonElement hit)
    {
        var index = hit.GetProperty("_index").GetString() ?? "";
        var id = hit.GetProperty("_id").GetString() ?? "";
        var score = hit.TryGetProperty("_score", out var scoreVal) ? scoreVal.GetDouble() : 0;
        var source = hit.GetProperty("_source");

        var type = index.Replace("commonhall-", "");
        var title = GetStringProperty(source, "title", "displayName", "originalName") ?? "Untitled";
        var excerpt = GetStringProperty(source, "teaserText", "bodyText", "jobTitle", "altText");

        string? highlightedTitle = null;
        string? highlightedExcerpt = null;

        if (hit.TryGetProperty("highlight", out var highlight))
        {
            highlightedTitle = GetHighlight(highlight, "title", "displayName", "originalName");
            highlightedExcerpt = GetHighlight(highlight, "teaserText", "bodyText");
        }

        var url = BuildUrl(type, source);
        var imageUrl = GetStringProperty(source, "teaserImageUrl", "profilePhotoUrl");
        var subtitle = BuildSubtitle(type, source);
        var date = GetDateProperty(source, "publishedAt", "updatedAt", "createdAt");

        return new SearchHit
        {
            Id = id,
            Type = type,
            Title = title,
            Excerpt = excerpt,
            HighlightedTitle = highlightedTitle,
            HighlightedExcerpt = highlightedExcerpt,
            Url = url,
            ImageUrl = imageUrl,
            Subtitle = subtitle,
            Date = date,
            Score = score
        };
    }

    private static string? GetStringProperty(JsonElement source, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (source.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        return null;
    }

    private static string? GetHighlight(JsonElement highlight, params string[] fieldNames)
    {
        foreach (var field in fieldNames)
        {
            if (highlight.TryGetProperty(field, out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                var fragments = arr.EnumerateArray().Select(f => f.GetString()).Where(s => !string.IsNullOrEmpty(s));
                var combined = string.Join("... ", fragments);
                if (!string.IsNullOrEmpty(combined))
                    return combined;
            }
        }
        return null;
    }

    private static string BuildUrl(string type, JsonElement source)
    {
        return type switch
        {
            "news" => $"/news/{GetStringProperty(source, "slug")}",
            "pages" => $"/spaces/{GetStringProperty(source, "spaceSlug")}/{GetStringProperty(source, "slug")}",
            "users" => $"/people/{GetStringProperty(source, "id")}",
            "files" => GetStringProperty(source, "url") ?? "#",
            _ => "#"
        };
    }

    private static string BuildSubtitle(string type, JsonElement source)
    {
        return type switch
        {
            "news" => GetStringProperty(source, "channelName") ?? "",
            "pages" => GetStringProperty(source, "spaceName") ?? "",
            "users" => string.Join(" • ", new[] { GetStringProperty(source, "department"), GetStringProperty(source, "location") }.Where(s => !string.IsNullOrEmpty(s))),
            "files" => GetStringProperty(source, "collectionName") ?? GetStringProperty(source, "mimeType") ?? "",
            _ => ""
        };
    }

    private static DateTimeOffset? GetDateProperty(JsonElement source, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (source.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(prop.GetString(), out var date))
                    return date;
            }
        }
        return null;
    }

    public async Task<List<SearchSuggestion>> SuggestAsync(string query, int limit = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<SearchSuggestion>();

        var searchBody = new
        {
            size = limit * 4, // Get more results to deduplicate
            query = new
            {
                multi_match = new
                {
                    query,
                    fields = new[] { "title^3", "displayName^3", "originalName^3", "teaserText", "email" },
                    type = "phrase_prefix"
                }
            },
            _source = new[] { "title", "displayName", "originalName", "slug", "spaceSlug", "teaserImageUrl", "profilePhotoUrl", "channelName", "department", "url" }
        };

        var json = JsonSerializer.Serialize(searchBody, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var indexStr = string.Join(",", SearchIndexConfiguration.AllIndexes);

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/{indexStr}/_search",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new List<SearchSuggestion>();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseSuggestions(responseJson, limit);
    }

    private static List<SearchSuggestion> ParseSuggestions(string json, int limit)
    {
        using var doc = JsonDocument.Parse(json);
        var suggestions = new List<SearchSuggestion>();

        if (!doc.RootElement.TryGetProperty("hits", out var hits) ||
            !hits.TryGetProperty("hits", out var hitsArray))
            return suggestions;

        foreach (var hit in hitsArray.EnumerateArray())
        {
            if (suggestions.Count >= limit) break;

            var index = hit.GetProperty("_index").GetString() ?? "";
            var id = hit.GetProperty("_id").GetString() ?? "";
            var source = hit.GetProperty("_source");

            var type = index.Replace("commonhall-", "");
            var title = GetStringProperty(source, "title", "displayName", "originalName") ?? "Untitled";
            var subtitle = BuildSubtitle(type, source);
            var imageUrl = GetStringProperty(source, "teaserImageUrl", "profilePhotoUrl");
            var url = BuildUrl(type, source);

            suggestions.Add(new SearchSuggestion
            {
                Id = id,
                Type = type,
                Title = title,
                Subtitle = subtitle,
                ImageUrl = imageUrl,
                Url = url
            });
        }

        return suggestions;
    }

    public async Task IndexNewsArticleAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var article = await _dbContext.NewsArticles
            .Include(a => a.Channel)
            .Include(a => a.Author)
            .Include(a => a.ArticleTags)
            .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

        if (article == null || article.IsDeleted)
        {
            await DeleteFromIndexAsync("news", articleId, cancellationToken);
            return;
        }

        var document = new NewsSearchDocument
        {
            Id = article.Id.ToString(),
            Title = article.Title,
            TeaserText = article.TeaserText,
            BodyText = ContentTextExtractor.ExtractText(article.Content),
            Tags = article.ArticleTags.Select(at => at.Tag.Name).ToList(),
            ChannelName = article.Channel?.Name,
            ChannelSlug = article.Channel?.Slug,
            SpaceName = article.Channel?.Space?.Name,
            SpaceSlug = article.Channel?.Space?.Slug,
            AuthorName = article.Author != null ? $"{article.Author.FirstName} {article.Author.LastName}".Trim() : null,
            AuthorId = article.AuthorId.ToString(),
            Status = article.Status.ToString(),
            PublishedAt = article.PublishedAt,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            Slug = article.Slug,
            ViewCount = article.ViewCount,
            TeaserImageUrl = article.TeaserImageUrl
        };

        await IndexDocumentAsync(SearchIndexConfiguration.NewsIndex, article.Id.ToString(), document, cancellationToken);
    }

    public async Task IndexPageAsync(Guid pageId, CancellationToken cancellationToken = default)
    {
        var page = await _dbContext.Pages
            .Include(p => p.Space)
            .FirstOrDefaultAsync(p => p.Id == pageId, cancellationToken);

        if (page == null || page.IsDeleted)
        {
            await DeleteFromIndexAsync("pages", pageId, cancellationToken);
            return;
        }

        var document = new PageSearchDocument
        {
            Id = page.Id.ToString(),
            Title = page.Title,
            BodyText = ContentTextExtractor.ExtractText(page.Content),
            SpaceName = page.Space?.Name,
            SpaceSlug = page.Space?.Slug,
            Slug = page.Slug,
            Status = page.Status.ToString(),
            CreatedAt = page.CreatedAt,
            UpdatedAt = page.UpdatedAt
        };

        await IndexDocumentAsync(SearchIndexConfiguration.PagesIndex, page.Id.ToString(), document, cancellationToken);
    }

    public async Task IndexUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            await DeleteFromIndexAsync("users", userId, cancellationToken);
            return;
        }

        var document = new UserSearchDocument
        {
            Id = user.Id.ToString(),
            DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Department = user.Department,
            Location = user.Location,
            JobTitle = user.JobTitle,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            IsActive = user.IsActive
        };

        await IndexDocumentAsync(SearchIndexConfiguration.UsersIndex, user.Id.ToString(), document, cancellationToken);
    }

    public async Task IndexFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await _dbContext.StoredFiles
            .Include(f => f.Collection)
            .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

        if (file == null || file.IsDeleted)
        {
            await DeleteFromIndexAsync("files", fileId, cancellationToken);
            return;
        }

        var document = new FileSearchDocument
        {
            Id = file.Id.ToString(),
            OriginalName = file.OriginalFileName,
            MimeType = file.MimeType,
            CollectionName = file.Collection?.Name,
            CollectionId = file.CollectionId?.ToString(),
            AltText = file.AltText,
            FileSize = file.FileSize,
            CreatedAt = file.CreatedAt,
            Url = file.Url
        };

        await IndexDocumentAsync(SearchIndexConfiguration.FilesIndex, file.Id.ToString(), document, cancellationToken);
    }

    private async Task IndexDocumentAsync<T>(string index, string id, T document, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(document, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"{_baseUrl}/{index}/_doc/{id}", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to index document {Id} in {Index}: {Error}", id, index, error);
        }
    }

    public async Task DeleteFromIndexAsync(string indexType, Guid id, CancellationToken cancellationToken = default)
    {
        var index = indexType.ToLower() switch
        {
            "news" => SearchIndexConfiguration.NewsIndex,
            "pages" => SearchIndexConfiguration.PagesIndex,
            "users" => SearchIndexConfiguration.UsersIndex,
            "files" => SearchIndexConfiguration.FilesIndex,
            _ => throw new ArgumentException($"Unknown index type: {indexType}")
        };

        var response = await _httpClient.DeleteAsync($"{_baseUrl}/{index}/_doc/{id}", cancellationToken);

        // 404 is OK - document might not exist
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to delete document {Id} from {Index}: {Error}", id, index, error);
        }
    }

    public async Task ReindexAllAsync(IProgress<ReindexProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        const int batchSize = 500;

        // Recreate indexes
        foreach (var index in SearchIndexConfiguration.AllIndexes)
        {
            await _httpClient.DeleteAsync($"{_baseUrl}/{index}", cancellationToken);
        }
        await EnsureIndexesExistAsync(cancellationToken);

        // Index news articles
        var newsCount = await _dbContext.NewsArticles.CountAsync(cancellationToken);
        var newsProcessed = 0;
        var newsQuery = _dbContext.NewsArticles
            .Include(a => a.Channel).ThenInclude(c => c!.Space)
            .Include(a => a.Author)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .Where(a => !a.IsDeleted)
            .OrderBy(a => a.Id);

        await foreach (var batch in newsQuery.AsAsyncEnumerable().Chunk(batchSize).WithCancellation(cancellationToken))
        {
            foreach (var article in batch)
            {
                await IndexNewsArticleAsync(article.Id, cancellationToken);
                newsProcessed++;
            }
            progress?.Report(new ReindexProgress
            {
                CurrentIndex = "news",
                ProcessedCount = newsProcessed,
                TotalCount = newsCount,
                Message = $"Indexing news articles: {newsProcessed}/{newsCount}"
            });
        }

        // Index pages
        var pagesCount = await _dbContext.Pages.CountAsync(p => !p.IsDeleted, cancellationToken);
        var pagesProcessed = 0;
        var pagesQuery = _dbContext.Pages
            .Include(p => p.Space)
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Id);

        await foreach (var batch in pagesQuery.AsAsyncEnumerable().Chunk(batchSize).WithCancellation(cancellationToken))
        {
            foreach (var page in batch)
            {
                await IndexPageAsync(page.Id, cancellationToken);
                pagesProcessed++;
            }
            progress?.Report(new ReindexProgress
            {
                CurrentIndex = "pages",
                ProcessedCount = pagesProcessed,
                TotalCount = pagesCount,
                Message = $"Indexing pages: {pagesProcessed}/{pagesCount}"
            });
        }

        // Index users
        var usersCount = await _dbContext.Users.CountAsync(cancellationToken);
        var usersProcessed = 0;
        var usersQuery = _dbContext.Users.OrderBy(u => u.Id);

        await foreach (var batch in usersQuery.AsAsyncEnumerable().Chunk(batchSize).WithCancellation(cancellationToken))
        {
            foreach (var user in batch)
            {
                await IndexUserAsync(user.Id, cancellationToken);
                usersProcessed++;
            }
            progress?.Report(new ReindexProgress
            {
                CurrentIndex = "users",
                ProcessedCount = usersProcessed,
                TotalCount = usersCount,
                Message = $"Indexing users: {usersProcessed}/{usersCount}"
            });
        }

        // Index files
        var filesCount = await _dbContext.StoredFiles.CountAsync(f => !f.IsDeleted, cancellationToken);
        var filesProcessed = 0;
        var filesQuery = _dbContext.StoredFiles
            .Include(f => f.Collection)
            .Where(f => !f.IsDeleted)
            .OrderBy(f => f.Id);

        await foreach (var batch in filesQuery.AsAsyncEnumerable().Chunk(batchSize).WithCancellation(cancellationToken))
        {
            foreach (var file in batch)
            {
                await IndexFileAsync(file.Id, cancellationToken);
                filesProcessed++;
            }
            progress?.Report(new ReindexProgress
            {
                CurrentIndex = "files",
                ProcessedCount = filesProcessed,
                TotalCount = filesCount,
                Message = $"Indexing files: {filesProcessed}/{filesCount}"
            });
        }

        progress?.Report(new ReindexProgress
        {
            CurrentIndex = "complete",
            ProcessedCount = newsProcessed + pagesProcessed + usersProcessed + filesProcessed,
            TotalCount = newsCount + pagesCount + usersCount + filesCount,
            Message = "Reindex complete"
        });
    }
}
