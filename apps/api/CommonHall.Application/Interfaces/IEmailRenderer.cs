using CommonHall.Domain.Entities;

namespace CommonHall.Application.Interfaces;

/// <summary>
/// Renders email newsletters to HTML with tracking pixels and link rewriting.
/// </summary>
public interface IEmailRenderer
{
    /// <summary>
    /// Renders a newsletter to responsive HTML email with inline styles.
    /// Adds tracking pixel and rewrites links for click tracking.
    /// </summary>
    Task<string> RenderToHtmlAsync(
        EmailNewsletter newsletter,
        EmailRecipient recipient,
        string baseUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a preview of the newsletter without tracking elements.
    /// </summary>
    Task<string> RenderPreviewAsync(
        EmailNewsletter newsletter,
        CancellationToken cancellationToken = default);
}
