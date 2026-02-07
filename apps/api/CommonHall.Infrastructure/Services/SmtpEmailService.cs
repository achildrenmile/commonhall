using System.Net;
using System.Net.Mail;
using CommonHall.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommonHall.Infrastructure.Services;

/// <summary>
/// SMTP-based email sending service for development.
/// </summary>
public sealed class SmtpEmailService : IEmailSendingService, IDisposable
{
    private readonly SmtpClient _client;
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;
    private bool _disposed;

    public SmtpEmailService(
        IOptions<SmtpSettings> settings,
        ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000 // 30 seconds
        };

        if (!string.IsNullOrEmpty(_settings.Username))
        {
            _client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }
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
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlContent,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(to));

            if (!string.IsNullOrEmpty(plainTextContent))
            {
                var plainView = AlternateView.CreateAlternateViewFromString(
                    plainTextContent,
                    null,
                    "text/plain");
                var htmlView = AlternateView.CreateAlternateViewFromString(
                    htmlContent,
                    null,
                    "text/html");

                message.AlternateViews.Add(plainView);
                message.AlternateViews.Add(htmlView);
            }

            await _client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipient}", to);
            return new EmailSendResult(true, MessageId: Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
            return new EmailSendResult(false, ErrorMessage: ex.Message);
        }
    }

    public async Task<IEnumerable<EmailSendResult>> SendBulkAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EmailSendResult>();

        foreach (var msg in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await SendAsync(
                msg.To,
                msg.Subject,
                msg.HtmlContent,
                msg.PlainTextContent,
                cancellationToken);

            results.Add(result with { RecipientId = msg.RecipientId });

            // Small delay to avoid overwhelming the SMTP server
            await Task.Delay(50, cancellationToken);
        }

        return results;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client.Dispose();
            _disposed = true;
        }
    }
}

public sealed class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool EnableSsl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "noreply@localhost";
    public string FromName { get; set; } = "CommonHall";
}
