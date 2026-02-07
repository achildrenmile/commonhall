using FluentValidation;
using MediatR;

namespace CommonHall.Application.Features.Pages.Commands;

public sealed record ReorderPagesCommand : IRequest
{
    public Guid SpaceId { get; init; }
    public required List<PageOrderItem> Pages { get; init; }
}

public sealed record PageOrderItem
{
    public Guid Id { get; init; }
    public int SortOrder { get; init; }
}

public sealed class ReorderPagesCommandValidator : AbstractValidator<ReorderPagesCommand>
{
    public ReorderPagesCommandValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty().WithMessage("Space ID is required.");

        RuleFor(x => x.Pages)
            .NotEmpty().WithMessage("At least one page must be provided.");

        RuleForEach(x => x.Pages).ChildRules(page =>
        {
            page.RuleFor(p => p.Id)
                .NotEmpty().WithMessage("Page ID is required.");
        });
    }
}
