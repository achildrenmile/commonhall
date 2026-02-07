namespace CommonHall.Application.Interfaces;

/// <summary>
/// Service for sending emails. Implementations include SMTP (dev) and SendGrid (prod).
/// </summary>
public interface IEmailSendingService
{
    /// <summary>
    /// Sends a single email.
    /// </summary>
    Task<EmailSendResult> SendAsync(
        string to,
        string subject,
        string htmlContent,
        string? plainTextContent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends emails in bulk with optimized batching.
    /// </summary>
    Task<IEnumerable<EmailSendResult>> SendBulkAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default);
}

public record EmailMessage(
    string To,
    string Subject,
    string HtmlContent,
    string? PlainTextContent = null,
    string? RecipientId = null);

public record EmailSendResult(
    bool Success,
    string? RecipientId = null,
    string? ErrorMessage = null,
    string? MessageId = null);
