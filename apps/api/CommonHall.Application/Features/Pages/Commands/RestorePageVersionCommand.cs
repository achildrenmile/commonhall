using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Pages.Commands;

public sealed record RestorePageVersionCommand : IRequest<PageDto>
{
    public Guid PageId { get; init; }
    public Guid VersionId { get; init; }
}

public sealed class RestorePageVersionCommandValidator : AbstractValidator<RestorePageVersionCommand>
{
    public RestorePageVersionCommandValidator()
    {
        RuleFor(x => x.PageId)
            .NotEmpty().WithMessage("Page ID is required.");

        RuleFor(x => x.VersionId)
            .NotEmpty().WithMessage("Version ID is required.");
    }
}
