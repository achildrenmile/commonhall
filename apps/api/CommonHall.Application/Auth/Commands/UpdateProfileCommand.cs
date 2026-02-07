using CommonHall.Application.DTOs;
using FluentValidation;
using MediatR;

namespace CommonHall.Application.Auth.Commands;

public sealed record UpdateProfileCommand : IRequest<UserDto>
{
    public string? DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
    public string? JobTitle { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
    public string? PreferredLanguage { get; init; }
}

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Display name must not exceed 200 characters.")
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.")
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.")
            .When(x => x.LastName is not null);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500).WithMessage("Avatar URL must not exceed 500 characters.")
            .When(x => x.AvatarUrl is not null);

        RuleFor(x => x.Department)
            .MaximumLength(200).WithMessage("Department must not exceed 200 characters.")
            .When(x => x.Department is not null);

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.")
            .When(x => x.Location is not null);

        RuleFor(x => x.JobTitle)
            .MaximumLength(200).WithMessage("Job title must not exceed 200 characters.")
            .When(x => x.JobTitle is not null);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage("Phone number must not exceed 50 characters.")
            .When(x => x.PhoneNumber is not null);

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must not exceed 2000 characters.")
            .When(x => x.Bio is not null);

        RuleFor(x => x.PreferredLanguage)
            .MaximumLength(10).WithMessage("Preferred language must not exceed 10 characters.")
            .When(x => x.PreferredLanguage is not null);
    }
}
