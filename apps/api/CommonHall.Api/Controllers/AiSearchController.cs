using System.Text;
using CommonHall.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiSearchController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly ISearchService _searchService;
    private readonly ILogger<AiSearchController> _logger;
    private const int MaxContextTokens = 4000;
    private const int EstimatedCharsPerToken = 4;

    public AiSearchController(
        IAiService aiService,
        ISearchService searchService,
        ILogger<AiSearchController> logger)
    {
        _aiService = aiService;
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Ask a question and get an AI-generated answer based on content in the system
    /// </summary>
    [HttpPost("ask")]
    public async Task Ask(
        [FromBody] AskRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            // Step 1: Search Elasticsearch for top 10 results
            var searchResult = await _searchService.SearchAsync(
                new SearchQuery
                {
                    Query = request.Question,
                    Size = 10
                },
                cancellationToken);

            // Step 2: Build context from search results (max 4000 tokens)
            var (context, sources) = BuildContext(searchResult.Hits);

            // Send sources first
            var sourcesJson = System.Text.Json.JsonSerializer.Serialize(sources);
            await Response.WriteAsync($"event: sources\ndata: {EscapeForSse(sourcesJson)}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            // Step 3: Build conversation with system prompt
            var systemPrompt = @"You are a helpful AI assistant for an enterprise intranet. Answer questions based ONLY on the provided context from internal content. If you cannot find the answer in the context, say so clearly.

Guidelines:
- Only use information from the provided context
- Cite your sources by referencing the document titles
- Be concise but thorough
- If multiple sources provide information, synthesize them
- If the context doesn't contain relevant information, say 'I couldn't find information about that in our internal content.'

Format your response in a clear, readable way. Use markdown formatting where appropriate.";

            var userPromptBuilder = new StringBuilder();

            // Add conversation history if provided
            if (request.ConversationHistory?.Any() == true)
            {
                userPromptBuilder.AppendLine("Previous conversation:");
                foreach (var message in request.ConversationHistory.TakeLast(5))
                {
                    userPromptBuilder.AppendLine($"{message.Role}: {message.Content}");
                }
                userPromptBuilder.AppendLine();
            }

            userPromptBuilder.AppendLine("Context from internal documents:");
            userPromptBuilder.AppendLine(context);
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine($"Question: {request.Question}");

            // Step 4: Stream the response
            await foreach (var chunk in _aiService.GenerateStreamingAsync(
                systemPrompt,
                userPromptBuilder.ToString(),
                new AiGenerationOptions { MaxTokens = 2048, Temperature = 0.3 },
                cancellationToken))
            {
                await Response.WriteAsync($"data: {EscapeForSse(chunk)}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI search request");
            await Response.WriteAsync($"event: error\ndata: {EscapeForSse("An error occurred while processing your request")}\n\n", cancellationToken);
        }
    }

    private (string context, List<SourceReference> sources) BuildContext(List<SearchHit> hits)
    {
        var contextBuilder = new StringBuilder();
        var sources = new List<SourceReference>();
        var currentTokens = 0;
        var maxChars = MaxContextTokens * EstimatedCharsPerToken;

        foreach (var hit in hits)
        {
            var excerpt = hit.Excerpt ?? hit.HighlightedExcerpt ?? "";
            // Strip HTML tags from highlighted excerpt
            excerpt = System.Text.RegularExpressions.Regex.Replace(excerpt, "<[^>]+>", "");

            var docEntry = $"[{hit.Type}: {hit.Title}]\n{excerpt}\n---\n";
            var docTokens = docEntry.Length / EstimatedCharsPerToken;

            if (currentTokens + docTokens > MaxContextTokens)
            {
                // Truncate to fit
                var remainingChars = maxChars - contextBuilder.Length;
                if (remainingChars > 100)
                {
                    contextBuilder.Append(docEntry[..Math.Min(docEntry.Length, remainingChars)]);
                    sources.Add(new SourceReference
                    {
                        Title = hit.Title,
                        Type = hit.Type,
                        Url = hit.Url ?? "#",
                        Excerpt = TruncateExcerpt(excerpt, 100)
                    });
                }
                break;
            }

            contextBuilder.Append(docEntry);
            currentTokens += docTokens;

            sources.Add(new SourceReference
            {
                Title = hit.Title,
                Type = hit.Type,
                Url = hit.Url ?? "#",
                Excerpt = TruncateExcerpt(excerpt, 150)
            });
        }

        return (contextBuilder.ToString(), sources);
    }

    private static string TruncateExcerpt(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..(maxLength - 3)] + "...";
    }

    private static string EscapeForSse(string text)
    {
        return text.Replace("\n", "\\n").Replace("\r", "");
    }
}

public record AskRequest(
    string Question,
    List<ConversationMessage>? ConversationHistory = null
);

public record ConversationMessage(string Role, string Content);

public record SourceReference
{
    public required string Title { get; init; }
    public required string Type { get; init; }
    public required string Url { get; init; }
    public string? Excerpt { get; init; }
}
