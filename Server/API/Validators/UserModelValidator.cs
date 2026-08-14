using FastEndpoints;
using FluentValidation;
using Server.API.Routes.Internal.Authentication;

namespace Server.API.Validations;

public class UserModelValidator : Validator<UserModel>
{
    public UserModelValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-post är obligatoriskt.")
            .EmailAddress().WithMessage("Ogiltigt e-postformat.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Lösenord är obligatoriskt.")
            .MinimumLength(8).WithMessage("Lösenord måste vara minst åtta tecken långt.")
            .Must(PasswordComplexity).WithMessage("Lösenord måste innehålla minst en versal, en gemen, en siffra och ett specialtecken.");
    }

    private bool PasswordComplexity(string password)
    {
        bool hasUpperCase = password.Any(char.IsUpper);
        bool hasLowerCase = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecialChar = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
    }
}