using System.Net.Http.Json;
using System.Text.Json;
using CommonHall.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommonHall.Infrastructure.Services;

/// <summary>
/// SendGrid-based email sending service for production.
/// </summary>
public sealed class SendGridEmailService : IEmailSendingService
{
    private readonly HttpClient _httpClient;
    private readonly SendGridSettings _settings;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        HttpClient httpClient,
        IOptions<SendGridSettings> settings,
        ILogger<SendGridEmailService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.sendgrid.com/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
    }

    public async Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string htmlContent,
        string? plainTextContent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = to } }
                    }
                },
                from = new
                {
                    email = _settings.FromEmail,
                    name = _settings.FromName
                },
                subject,
                content = new object[]
                {
                    new { type = "text/plain", value = plainTextContent ?? StripHtml(htmlContent) },
                    new { type = "text/html", value = htmlContent }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                "v3/mail/send",
                payload,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var messageId = response.Headers.TryGetValues("X-Message-Id", out var values)
                    ? values.FirstOrDefault()
                    : null;

                _logger.LogInformation("Email sent via SendGrid to {Recipient}", to);
                return new EmailSendResult(true, MessageId: messageId);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SendGrid API error: {StatusCode} - {Error}",
                response.StatusCode, errorContent);

            return new EmailSendResult(false, ErrorMessage: errorContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SendGrid to {Recipient}", to);
            return new EmailSendResult(false, ErrorMessage: ex.Message);
        }
    }

    public async Task<IEnumerable<EmailSendResult>> SendBulkAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EmailSendResult>();
        var messageList = messages.ToList();

        // SendGrid allows up to 1000 personalizations per request
        // We batch by 100 for safety and to maintain individual results
        const int batchSize = 100;

        for (var i = 0; i < messageList.Count; i += batchSize)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var batch = messageList.Skip(i).Take(batchSize).ToList();

            // For SendGrid bulk, we still need to send individually for now
            // because each email might have different content (personalized tracking URLs)
            foreach (var msg in batch)
            {
                var result = await SendAsync(
                    msg.To,
                    msg.Subject,
                    msg.HtmlContent,
                    msg.PlainTextContent,
                    cancellationToken);

                results.Add(result with { RecipientId = msg.RecipientId });
            }

            // Rate limiting - SendGrid has limits of 600/minute on free tier
            if (i + batchSize < messageList.Count)
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        return results;
    }

    private static string StripHtml(string html)
    {
        // Simple HTML stripping - in production use a proper library
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ")
            .Replace("  ", " ")
            .Trim();
    }
}

public sealed class SendGridSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "CommonHall";
}
