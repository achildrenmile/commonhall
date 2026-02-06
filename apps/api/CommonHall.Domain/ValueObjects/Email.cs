using System.Text.RegularExpressions;

namespace CommonHall.Domain.ValueObjects;

public partial record Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        email = email.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        return new Email(email);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
