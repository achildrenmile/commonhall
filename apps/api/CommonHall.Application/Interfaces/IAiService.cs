namespace CommonHall.Application.Interfaces;

public interface IAiService
{
    Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        AiGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> GenerateStreamingAsync(
        string systemPrompt,
        string userPrompt,
        AiGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<ClassificationResult> ClassifyAsync(
        string text,
        string[] categories,
        CancellationToken cancellationToken = default);

    Task<List<ClassificationResult>> ClassifyBatchAsync(
        string[] texts,
        string[] categories,
        CancellationToken cancellationToken = default);
}

public record AiGenerationOptions
{
    public int MaxTokens { get; init; } = 1024;
    public double Temperature { get; init; } = 0.7;
    public string? Model { get; init; }
}

public record ClassificationResult
{
    public required string Category { get; init; }
    public double Confidence { get; init; }
    public string? Reasoning { get; init; }
}
