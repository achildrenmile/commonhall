using System.Text;
using CommonHall.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[ApiController]
[Route("api/v1/ai/companion")]
[Authorize]
public class AiCompanionController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly ILogger<AiCompanionController> _logger;

    public AiCompanionController(IAiService aiService, ILogger<AiCompanionController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Generate headline suggestions for an article
    /// </summary>
    [HttpPost("headline")]
    public async Task<IActionResult> GenerateHeadlines(
        [FromBody] HeadlineRequest request,
        CancellationToken cancellationToken)
    {
        var systemPrompt = @"You are an expert content editor for an internal communications platform. Generate compelling, clear headlines that will engage employees.

Guidelines:
- Create 5 distinct headline variations
- Headlines should be concise (under 80 characters)
- Use active voice
- Make them informative yet engaging
- Match the specified tone if provided

Respond with just the headlines, one per line, no numbering or bullets.";

        var userPrompt = $"Generate headlines for this article content:\n\n{request.ArticleBody}";
        if (!string.IsNullOrEmpty(request.Tone))
        {
            userPrompt += $"\n\nTone: {request.Tone}";
        }

        var response = await _aiService.GenerateAsync(
            systemPrompt,
            userPrompt,
            new AiGenerationOptions { MaxTokens = 512, Temperature = 0.8 },
            cancellationToken);

        var headlines = response
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(h => h.Trim())
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Take(5)
            .ToList();

        return Ok(new { data = new { headlines } });
    }

    /// <summary>
    /// Generate a teaser/summary for an article
    /// </summary>
    [HttpPost("teaser")]
    public async Task<IActionResult> GenerateTeaser(
        [FromBody] TeaserRequest request,
        CancellationToken cancellationToken)
    {
        var maxLength = request.MaxLength ?? 200;

        var systemPrompt = $@"You are an expert content editor. Generate a compelling teaser/summary that will make employees want to read the full article.

Guidelines:
- Maximum {maxLength} characters
- Capture the key value proposition
- Use engaging language
- Create curiosity without clickbait

Respond with just the teaser text, nothing else.";

        var response = await _aiService.GenerateAsync(
            systemPrompt,
            $"Generate a teaser for this article:\n\n{request.ArticleBody}",
            new AiGenerationOptions { MaxTokens = 256, Temperature = 0.7 },
            cancellationToken);

        var teaser = response.Trim();
        if (teaser.Length > maxLength)
        {
            teaser = teaser[..(maxLength - 3)] + "...";
        }

        return Ok(new { data = new { teaser } });
    }

    /// <summary>
    /// Improve text based on instructions (streaming)
    /// </summary>
    [HttpPost("improve")]
    public async Task ImproveText(
        [FromBody] ImproveRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var systemPrompt = @"You are an expert editor helping improve internal communications content. Apply the user's instruction to improve the text while maintaining its core message.

Guidelines:
- Follow the instruction precisely
- Maintain professional tone appropriate for employee communications
- Preserve key information
- Output only the improved text, no explanations";

        var userPrompt = $"Instruction: {request.Instruction}\n\nText to improve:\n{request.Text}";

        await foreach (var chunk in _aiService.GenerateStreamingAsync(
            systemPrompt,
            userPrompt,
            new AiGenerationOptions { MaxTokens = 2048, Temperature = 0.7 },
            cancellationToken))
        {
            await Response.WriteAsync($"data: {EscapeForSse(chunk)}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
    }

    /// <summary>
    /// Summarize text to a specified length
    /// </summary>
    [HttpPost("summarize")]
    public async Task<IActionResult> Summarize(
        [FromBody] SummarizeRequest request,
        CancellationToken cancellationToken)
    {
        var lengthGuide = request.Length?.ToLower() switch
        {
            "short" => "1-2 sentences",
            "medium" => "3-4 sentences",
            "long" => "a short paragraph (5-7 sentences)",
            _ => "3-4 sentences"
        };

        var systemPrompt = $@"You are an expert summarizer. Create a {lengthGuide} summary of the provided text.

Guidelines:
- Capture the main points
- Use clear, concise language
- Maintain accuracy to the original
- Output only the summary";

        var response = await _aiService.GenerateAsync(
            systemPrompt,
            $"Summarize this text:\n\n{request.Text}",
            new AiGenerationOptions { MaxTokens = 512, Temperature = 0.5 },
            cancellationToken);

        return Ok(new { data = new { summary = response.Trim() } });
    }

    /// <summary>
    /// Translate text to a target language
    /// </summary>
    [HttpPost("translate")]
    public async Task<IActionResult> Translate(
        [FromBody] TranslateRequest request,
        CancellationToken cancellationToken)
    {
        var systemPrompt = $@"You are a professional translator. Translate the text to {request.TargetLanguage}.

Guidelines:
- Maintain the meaning and tone
- Use natural language appropriate for the target language
- Preserve formatting
- Output only the translation";

        var response = await _aiService.GenerateAsync(
            systemPrompt,
            $"Translate:\n\n{request.Text}",
            new AiGenerationOptions { MaxTokens = 2048, Temperature = 0.3 },
            cancellationToken);

        return Ok(new { data = new { translation = response.Trim() } });
    }

    /// <summary>
    /// Draft content from a briefing (streaming)
    /// </summary>
    [HttpPost("draft-from-briefing")]
    public async Task DraftFromBriefing(
        [FromBody] BriefingRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var systemPrompt = @"You are an expert internal communications writer. Create engaging, professional content for an employee intranet based on the provided briefing.

Guidelines:
- Match the specified purpose and tone
- Write for the target audience
- Include all key points
- Use clear, engaging language
- Structure with appropriate headings if the content is substantial
- Make it scannable with short paragraphs";

        var briefingBuilder = new StringBuilder();
        briefingBuilder.AppendLine($"Purpose: {request.Purpose}");
        briefingBuilder.AppendLine($"Target Audience: {request.Audience}");
        briefingBuilder.AppendLine($"Tone: {request.Tone}");
        briefingBuilder.AppendLine($"\nKey Points to Cover:");
        foreach (var point in request.KeyPoints)
        {
            briefingBuilder.AppendLine($"- {point}");
        }

        if (request.AttachmentTexts?.Any() == true)
        {
            briefingBuilder.AppendLine($"\nReference Materials:");
            foreach (var text in request.AttachmentTexts)
            {
                briefingBuilder.AppendLine($"---\n{text}\n---");
            }
        }

        await foreach (var chunk in _aiService.GenerateStreamingAsync(
            systemPrompt,
            $"Create content based on this briefing:\n\n{briefingBuilder}",
            new AiGenerationOptions { MaxTokens = 4096, Temperature = 0.7 },
            cancellationToken))
        {
            await Response.WriteAsync($"data: {EscapeForSse(chunk)}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
    }

    private static string EscapeForSse(string text)
    {
        return text.Replace("\n", "\\n").Replace("\r", "");
    }
}

public record HeadlineRequest(string ArticleBody, string? Tone = null);
public record TeaserRequest(string ArticleBody, int? MaxLength = null);
public record ImproveRequest(string Text, string Instruction);
public record SummarizeRequest(string Text, string? Length = null);
public record TranslateRequest(string Text, string TargetLanguage);
public record BriefingRequest(
    string Purpose,
    string Audience,
    string Tone,
    List<string> KeyPoints,
    List<string>? AttachmentTexts = null
);
