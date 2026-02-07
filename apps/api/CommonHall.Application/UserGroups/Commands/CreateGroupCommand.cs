using CommonHall.Application.DTOs;
using CommonHall.Domain.Enums;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.UserGroups.Commands;

public sealed record CreateGroupCommand : IRequest<UserGroupDto>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public GroupType Type { get; init; } = GroupType.Manual;
    public string? RuleDefinition { get; init; }
}

public sealed class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MinimumLength(2).WithMessage("Group name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Group name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);
    }
}
